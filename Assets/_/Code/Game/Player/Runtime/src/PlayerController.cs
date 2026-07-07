using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Orchestrates the first-person player character: reads input, resolves the
    /// active state, then drives movement and stealth noise. Receives its tick from
    /// the <see cref="UpdateManager"/> instead of MonoBehaviour.Update, and gets all
    /// of its collaborators injected through the Inspector.
    /// Adapted from the third-person controller: movement is body-relative (mouse-look
    /// rotates the body yaw in <see cref="PlayerLook"/>, so the body never turns to
    /// face its movement direction) and cover entry/exit is an explicit button press
    /// instead of automatic wall-push detection. All camera framing (crouch/crawl
    /// height, cover glue and corner peek) is owned by <see cref="PlayerCameraRig"/>;
    /// this controller only exposes the cover state the rig reads.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private WallSensor _wallSensor;
        [SerializeField] private NoiseEmitter _noiseEmitter;
        [SerializeField] private CoverContextEventChannelSO _coverContextChannel;
        [SerializeField] private VoidEventChannelSO _caughtChannel;

        [Header("Movement Speeds (m/s)")]
        [SerializeField] private float _runSpeed = 5.5f;
        [SerializeField] private float _crouchSpeed = 2.0f;
        [SerializeField] private float _crawlSpeed = 1.2f;
        [SerializeField] private float _wallHugSpeed = 1.5f;

        [Header("Analog Locomotion")]
        [SerializeField, Range(0.0f, 1.0f)] private float _runThreshold = 0.85f;

        [Header("Posture Transition Locks (s)")]
        [Tooltip("Movement is suppressed for this long when the posture changes, so the " +
                 "character cannot run at full speed while a stand-up / cover transition plays.")]
        [SerializeField] private float _crawlEnterLock = 0.8f;
        [SerializeField] private float _crawlExitLock = 0.8f;
        [SerializeField] private float _crouchEnterLock = 0.4f;
        [SerializeField] private float _crouchExitLock = 0.4f;
        [SerializeField] private float _coverEnterLock = 0.2f;
        [SerializeField] private float _coverExitLock = 0.2f;

        [Header("Wall Hug")]
        [SerializeField] private float _coverFacingDeadzone = 0.5f;

        #endregion


        #region Properties

        /// <summary>Current logical state of the player.</summary>
        public PlayerState CurrentState => _currentState;

        /// <summary>True while the player is hugging a wall (in cover).</summary>
        public bool IsInCover => _currentState == PlayerState.WallHugging;

        /// <summary>Averaged normal of the wall being hugged (valid while <see cref="IsInCover"/>).</summary>
        public Vector3 CoverWallNormal => _wallSensor.WallNormal;

        /// <summary>
        /// Side the player is currently sneaking toward along the wall: true = the
        /// wall's right (+wallRight), false = its left. Drives which way the camera
        /// looks and which corner it peeks around.
        /// </summary>
        public bool CoverFacingRight => _coverFacingRight;

        #endregion


        #region Public API

        /// <summary>Forces or clears the knocked-out state (e.g. when the player is caught).</summary>
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

            _noiseEmitter.Emit(_currentState);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _updateManager.Register(this);

            if (_caughtChannel == null) return;
            _caughtChannel.OnEventRaised += OnCaught;
        }

        private void OnDisable()
        {
            _updateManager.Unregister(this);

            if (_caughtChannel == null) return;
            _caughtChannel.OnEventRaised -= OnCaught;
        }

        #endregion


        #region Private Fields

        private readonly PlayerStateMachine _stateMachine = new();
        private PlayerState _currentState = PlayerState.Idle;
        private PlayerState _previousState = PlayerState.Idle;
        private float _transitionLockTimer;
        private bool _isKnockedOut;
        private bool _wasInCover;
        private bool _coverFacingRight;
        private bool _coverFlipHeldPrev;

        #endregion


        #region Private Constants

        private const float MoveThreshold = 0.0001f;

        #endregion


        #region Private Methods

        // Being detected no longer freezes the player: the game is about staying in
        // control and fleeing to a hiding spot. Only actually being caught by a
        // chasing enemy (the catch channel, raised by EnemyChase) knocks the player out.
        private void OnCaught() => SetKnockedOut(true);

        // FPS movement is body-relative: mouse-look rotates the player's yaw in
        // PlayerLook (the body itself does not pitch), so the stick's forward/right
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

        // While hugging, movement is wall-relative, not body- or camera-relative:
        // North advances along the current sneak direction, South flips it. The body
        // is intentionally NOT rotated in cover - the camera rig frames along the wall
        // on its own, and leaving the body yaw untouched keeps free-look seamless when
        // cover is released. West/East stay free (exit is the explicit cover button).
        private void ApplyWallHugMovement(Vector2 rawInput, bool movementLocked, float deltaTime)
        {
            Vector3 wallNormal = _wallSensor.WallNormal;
            Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal);

            // South (pull back) flips the sneak direction, edge-triggered so a held
            // stick flips only once per press.
            bool flipHeld = rawInput.y < -_coverFacingDeadzone;
            if (flipHeld && !_coverFlipHeldPrev) _coverFacingRight = !_coverFacingRight;
            _coverFlipHeldPrev = flipHeld;

            Vector3 travel = _coverFacingRight ? wallRight : -wallRight;
            float advance = movementLocked ? 0.0f : Mathf.Max(0.0f, rawInput.y);
            bool isAdvancing = advance > MoveThreshold;

            _motor.Move(isAdvancing ? travel : Vector3.zero, isAdvancing ? _wallHugSpeed * advance : 0.0f, deltaTime);
        }

        // Broadcasts cover enter/exit transitions so decoupled listeners (prompt UI,
        // noise ping input) can react without a direct reference to the player. Fires
        // only on transitions, not every frame.
        private void UpdateCoverContext(bool isInCover)
        {
            if (isInCover == _wasInCover) return;

            _wasInCover = isInCover;

            if (!isInCover) _coverFlipHeldPrev = false;

            if (_coverContextChannel == null) return;
            _coverContextChannel.Raise(new CoverContext(isInCover, transform.position, transform.position));
        }

        // While hugging, probe straight into the wall using its cached normal so
        // detection stays stable. Otherwise probe ahead of the character to acquire
        // a new wall.
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
        // The lock duration is sized to the matching transition so the character
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

        #endregion
    }
}