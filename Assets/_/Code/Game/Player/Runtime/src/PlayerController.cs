using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Orchestrates the first-person player character: reads input, resolves the
    /// active state, then drives movement, camera lean and stealth noise. Receives
    /// its tick from the <see cref="UpdateManager"/> instead of MonoBehaviour.Update,
    /// and gets all of its collaborators injected through the Inspector.
    /// Adapted from the third-person controller: movement is body-forward based
    /// (mouse-look rotates the body yaw elsewhere, so the body no longer turns to
    /// face movement direction), cover entry/exit is an explicit button press
    /// instead of automatic wall-push detection, and corner peek offsets a local
    /// camera pivot instead of repositioning an external third-person camera rig.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private WallSensor _wallSensor;
        [SerializeField] private CornerSensor _cornerSensor;
        [SerializeField] private PlayerAnimatorDriver _animatorDriver;
        [SerializeField] private NoiseEmitter _noiseEmitter;
        [SerializeField] private CoverContextEventChannelSO _coverContextChannel;
        [SerializeField] private VoidEventChannelSO _alarmChannel;

        [Header("First-Person Camera")]
        [Tooltip("Child transform the FPS camera (or its Cinemachine vcam) follows. Its rest local position is captured on Awake.")]
        [SerializeField] private Transform _cameraPivot;

        [Header("Movement Speeds (m/s)")]
        [SerializeField] private float _runSpeed = 5.5f;
        [SerializeField] private float _crouchSpeed = 2.0f;
        [SerializeField] private float _crawlSpeed = 1.2f;
        [SerializeField] private float _wallHugSpeed = 1.5f;

        [Header("Analog Locomotion")]
        [SerializeField, Range(0.0f, 1.0f)] private float _runThreshold = 0.85f;

        [Header("Posture Transition Locks (s)")]
        [Tooltip("Movement is suppressed for this long when the posture changes, so the " +
                 "character cannot run at full speed while a stand-up / cover transition clip plays.")]
        [SerializeField] private float _crawlEnterLock = 0.8f;
        [SerializeField] private float _crawlExitLock = 0.8f;
        [SerializeField] private float _crouchEnterLock = 0.4f;
        [SerializeField] private float _crouchExitLock = 0.4f;
        [SerializeField] private float _coverEnterLock = 0.2f;
        [SerializeField] private float _coverExitLock = 0.2f;

        [Header("Wall Hug")]
        [SerializeField] private float _coverFacingDeadzone = 0.5f;

        [Header("Corner Peek (Camera Lean)")]
        [SerializeField] private float _peekSpeed = 4.0f;
        [SerializeField] private float _peekSideShift = 0.5f;
        [SerializeField] private float _peekForwardShift = 0.3f;

        #endregion


        #region Properties

        /// <summary>Current logical state of the player.</summary>
        public PlayerState CurrentState => _currentState;

        #endregion


        #region Public API

        /// <summary>Forces or clears the knocked-out state (e.g. when the player is hit).</summary>
        public void SetKnockedOut(bool value) => _isKnockedOut = value;

        public void Tick(float deltaTime)
        {
            Vector2 rawInput = _input.MoveInput;
            Vector3 moveDirection = ResolveMoveDirection(rawInput);
            bool isMoving = moveDirection.sqrMagnitude > MoveThreshold;

            // Analog locomotion: the planar move speed is proportional to the left
            // stick / WASD tilt (0..1). Full tilt (or the run key) counts as running.
            float inputMagnitude = Mathf.Clamp01(rawInput.magnitude);
            bool wantsRun = _input.RunHeld || inputMagnitude >= _runThreshold;

            _wallSensor.Check(transform, ResolveWallProbeDirection());

            bool wantsCover = ResolveWantsCover();
            UpdateCoverContext(wantsCover);

            _currentState = _stateMachine.Evaluate(
                _isKnockedOut,
                wantsCover,
                _wallSensor.HasWall,
                _input.CrawlActive,
                _input.CrouchActive,
                isMoving,
                wantsRun);

            UpdateTransitionLock(deltaTime);
            bool movementLocked = _transitionLockTimer > 0.0f;

            ApplyMovement(moveDirection, inputMagnitude, rawInput, isMoving, movementLocked, deltaTime);

            if (_animatorDriver != null)
            {
                _animatorDriver.Apply(_currentState, _input.CrouchActive, ResolveLocomotionMagnitude(_currentState, inputMagnitude, isMoving), deltaTime);
            }

            _noiseEmitter.Emit(_currentState);
        }

        #endregion


        #region Unity Callbacks

        private void Awake()
        {
            if (_cameraPivot != null) _pivotRestLocalPosition = _cameraPivot.localPosition;
        }

        private void OnEnable()
        {
            _updateManager.Register(this);

            if (_alarmChannel == null) return;
            _alarmChannel.OnEventRaised += OnAlarmRaised;
        }

        private void OnDisable()
        {
            _updateManager.Unregister(this);

            if (_alarmChannel == null) return;
            _alarmChannel.OnEventRaised -= OnAlarmRaised;
        }

        #endregion


        #region Private Fields

        private readonly PlayerStateMachine _stateMachine = new();
        private PlayerState _currentState = PlayerState.Idle;
        private PlayerState _previousState = PlayerState.Idle;
        private float _transitionLockTimer;
        private bool _isKnockedOut;
        private float _peekAmount;
        private float _coverAdvance;
        private bool _wasInCover;
        private bool _coverFacingRight;
        private bool _coverFlipHeldPrev;
        private Vector3 _pivotRestLocalPosition;

        #endregion


        #region Private Constants

        private const float MoveThreshold = 0.0001f;

        #endregion


        #region Private Methods

        private void OnAlarmRaised() => SetKnockedOut(true);

        // FPS movement is body-relative: mouse-look rotates the player's yaw
        // elsewhere (the body itself does not pitch), so the stick's forward/right
        // axes map directly onto the body's own axes without any ground projection.
        private Vector3 ResolveMoveDirection(Vector2 rawInput) =>
            transform.right * rawInput.x + transform.forward * rawInput.y;

        private void ApplyMovement(Vector3 moveDirection, float inputMagnitude, Vector2 rawInput, bool isMoving, bool movementLocked, float deltaTime)
        {
            if (_currentState == PlayerState.KnockedOut)
            {
                _motor.Move(Vector3.zero, 0.0f, deltaTime);
                return;
            }

            if (_currentState == PlayerState.WallHugging)
            {
                ApplyWallHugMovement(rawInput, movementLocked, deltaTime);
                return;
            }

            // Standing, crouching and crawling are all analog (input-magnitude driven);
            // each has its own top speed. Body rotation is NOT driven here: in first
            // person, yaw follows the mouse-look component, not the movement
            // direction, so strafing does not turn the character.
            float maxSpeed = _currentState switch
            {
                PlayerState.Crawling => _crawlSpeed,
                PlayerState.Crouching => _crouchSpeed,
                _ => _runSpeed
            };
            float speed = movementLocked ? 0.0f : inputMagnitude * maxSpeed;
            _motor.Move(moveDirection.normalized, isMoving ? speed : 0.0f, deltaTime);
        }

        // While hugging, the character keeps its back to the wall (facing outward).
        // Controls are wall-relative, not camera-relative: North advances along the
        // current sneak direction, South flips that direction. West/East no longer
        // exit cover here since exit is now the explicit cover button (see
        // ResolveWantsCover), which keeps strafing free for peeking and aiming.
        private void ApplyWallHugMovement(Vector2 rawInput, bool movementLocked, float deltaTime)
        {
            Vector3 wallNormal = _wallSensor.WallNormal;
            Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal);

            // South (pull back) flips the sneak direction, edge-triggered so a held
            // stick flips only once per press. The flip fires the one-shot turn clip.
            bool flipHeld = rawInput.y < -_coverFacingDeadzone;
            if (flipHeld && !_coverFlipHeldPrev)
            {
                _coverFacingRight = !_coverFacingRight;
                if (_animatorDriver != null) _animatorDriver.TriggerCoverTurn();
            }
            _coverFlipHeldPrev = flipHeld;

            Vector3 travel = _coverFacingRight ? wallRight : -wallRight;
            float advance = movementLocked ? 0.0f : Mathf.Max(0.0f, rawInput.y);
            bool isAdvancing = advance > MoveThreshold;

            _motor.Move(isAdvancing ? travel : Vector3.zero, isAdvancing ? _wallHugSpeed * advance : 0.0f, deltaTime);
            _motor.FaceTowards(wallNormal, deltaTime);

            // Advance magnitude (0..1) freezes the cover-sneak clip when idle via the
            // state's Speed multiplier; facing picks the left/right sneak, look and turn.
            _coverAdvance = advance;

            UpdateCornerPeek(isAdvancing, deltaTime);

            float facing = _coverFacingRight ? 1.0f : -1.0f;
            if (_animatorDriver != null) _animatorDriver.SetCoverPeek(_peekAmount, facing);
        }

        // Leans the first-person camera pivot sideways (and slightly forward) around
        // an available outer corner. Offsets are expressed in the pivot's own local
        // axes, so they work regardless of the body's current world rotation - no
        // world-space camera repositioning or external rig is needed in first person.
        private void UpdateCornerPeek(bool isAdvancing, float deltaTime)
        {
            bool cornerAvailable = _cornerSensor != null &&
                (_coverFacingRight ? _cornerSensor.HasRightCorner : _cornerSensor.HasLeftCorner);
            bool wantsPeek = cornerAvailable && !isAdvancing;

            _peekAmount = Mathf.MoveTowards(_peekAmount, wantsPeek ? 1.0f : 0.0f, _peekSpeed * deltaTime);

            if (_cornerSensor != null) _cornerSensor.Check(transform, _wallSensor.WallNormal);
            if (_cameraPivot == null) return;

            float facing = _coverFacingRight ? 1.0f : -1.0f;
            Vector3 lean = Vector3.right * (facing * _peekSideShift * _peekAmount) + Vector3.forward * (_peekForwardShift * _peekAmount);
            _cameraPivot.localPosition = _pivotRestLocalPosition + lean;
        }

        // Broadcasts cover enter/exit transitions so decoupled listeners (prompt UI,
        // noise ping input) can react without a direct reference to the player. Unlike
        // the third-person version this only fires on transitions, not every frame,
        // since the camera itself is no longer driven through this channel.
        private void UpdateCoverContext(bool isInCover)
        {
            if (isInCover == _wasInCover) return;

            _wasInCover = isInCover;

            if (!isInCover)
            {
                _peekAmount = 0.0f;
                _coverFlipHeldPrev = false;
                if (_animatorDriver != null) _animatorDriver.SetCoverPeek(0.0f, _coverFacingRight ? 1.0f : -1.0f);
                if (_cameraPivot != null) _cameraPivot.localPosition = _pivotRestLocalPosition;
            }

            if (_coverContextChannel == null) return;
            _coverContextChannel.Raise(new CoverContext(isInCover, transform.position, transform.position));
        }

        // While hugging, probe straight into the wall using its cached normal, so
        // detection stays stable while the body rotates to face outward. Otherwise
        // probe ahead of the character to acquire a new wall.
        private Vector3 ResolveWallProbeDirection() =>
            _currentState == PlayerState.WallHugging ? -_wallSensor.WallNormal : transform.forward;

        // Cover is an explicit toggle: pressing the cover button next to a wall enters
        // cover, pressing it again while in cover exits. Losing the wall (e.g. backing
        // off an edge) still force-exits automatically as a safety net.
        private bool ResolveWantsCover()
        {
            bool pressed = _input.WallHugPressed;

            if (_currentState == PlayerState.WallHugging)
            {
                if (!_wallSensor.HasWall) return false;

                return !pressed;
            }

            if (!_wallSensor.HasWall) return false;

            return pressed;
        }

        // Starts a movement lock whenever the posture changes, then counts it down.
        // The lock duration is sized to the matching transition clip so the character
        // cannot move while standing up or entering/leaving cover.
        private void UpdateTransitionLock(float deltaTime)
        {
            if (_currentState != _previousState)
            {
                _transitionLockTimer = ResolveTransitionLock(_previousState, _currentState);
                _previousState = _currentState;
            }

            if (_transitionLockTimer > 0.0f) _transitionLockTimer -= deltaTime;
        }

        private float ResolveTransitionLock(PlayerState from, PlayerState to)
        {
            if (to == PlayerState.KnockedOut) return 0.0f;
            if (to == PlayerState.Crawling) return _crawlEnterLock;
            if (from == PlayerState.Crawling) return _crawlExitLock;
            if (to == PlayerState.Crouching) return _crouchEnterLock;
            if (from == PlayerState.Crouching) return _crouchExitLock;
            if (to == PlayerState.WallHugging) return _coverEnterLock;
            if (from == PlayerState.WallHugging) return _coverExitLock;

            return 0.0f;
        }

        // Normalized planar speed (0..1) fed to the locomotion blend tree. Standing and
        // crawling report the analog stick tilt directly, so their clips play at a rate
        // proportional to movement and freeze when idle (via the state Speed multiplier).
        // Cover reports its own advance, and everything returns 0 when idle or knocked
        // out so the trees settle on their resting pose.
        private float ResolveLocomotionMagnitude(PlayerState state, float inputMagnitude, bool isMoving)
        {
            if (state == PlayerState.KnockedOut) return 0.0f;
            if (!isMoving) return 0.0f;
            if (state == PlayerState.WallHugging) return _coverAdvance;

            return inputMagnitude;
        }

        #endregion
    }
}
