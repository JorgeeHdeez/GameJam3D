using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Translates the player's logical state into Animator parameters. The Animator
    /// is injected through the Inspector; this driver never searches for it.
    /// </summary>
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Animator _animator;
        [SerializeField] private float _speedDampTime = 0.12f;

        #endregion


        #region Public API

        /// <summary>
        /// Pushes the current state and a normalized planar speed (0..1) to the
        /// Animator. The speed is damped over <paramref name="deltaTime"/> so the
        /// locomotion blend tree transitions smoothly instead of snapping.
        /// <paramref name="isCrouching"/> is the crouch *intent* (not the state), so it
        /// stays true while crouched in cover and drives the standing/crouched switch.
        /// </summary>
        public void Apply(PlayerState state, bool isCrouching, float normalizedSpeed, float deltaTime)
        {
            _animator.SetFloat(SpeedParameter, normalizedSpeed, _speedDampTime, deltaTime);
            _animator.SetInteger(StateParameter, (int)state);
            _animator.SetBool(CrouchingParameter, isCrouching);
            _animator.SetBool(CrawlingParameter, state == PlayerState.Crawling);
            _animator.SetBool(WallHuggingParameter, state == PlayerState.WallHugging);
            _animator.SetBool(KnockedOutParameter, state == PlayerState.KnockedOut);

            // Posture of the *previous* frame (0 stand, 1 crouch, 2 prone): sub-state
            // machine entries use it to pick the right transition-in clip (e.g.
            // CrouchToCrawl vs StandToCrawl), since the live intent has already
            // flipped by the time the entry is evaluated.
            _animator.SetInteger(PosturePreviousParameter, _previousPosture);

            // Remember the posture while alive so the death animation matches it; the
            // value freezes when the knocked-out state is entered.
            int posture = ResolveDeathPosture(state, isCrouching);
            if (state != PlayerState.KnockedOut)
                _animator.SetInteger(DeathPostureParameter, posture);

            _previousPosture = posture;
        }

        /// <summary>
        /// Pushes the corner-peek magnitude (0 = flush against the wall, 1 = fully
        /// leaning past the corner) and the current cover facing (-1 = left, +1 = right)
        /// used to pick the left/right peek and turn clips.
        /// </summary>
        public void SetCoverPeek(float peek, float facing)
        {
            _animator.SetFloat(PeekParameter, peek);
            _animator.SetFloat(CoverFacingParameter, facing);
        }

        /// <summary>Fires the one-shot cover turn played when the sneak direction flips.</summary>
        public void TriggerCoverTurn() => _animator.SetTrigger(CoverTurnParameter);

        /// <summary>
        /// True while the current animator state is a posture-change clip tagged
        /// "PostureLock" (stand up, drop to prone, etc.). The controller suppresses
        /// movement during these so the character cannot slide or sprint mid-transition.
        /// The lock follows the clip exactly, releasing when it blends out.
        /// </summary>
        //public bool IsPostureLocked => _animator.GetCurrentAnimatorStateInfo(0).IsTag(PostureLockTag);

        #endregion


        #region Private Constants

        // Cached parameter hashes (static readonly is immutable, not mutable state).
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int StateParameter = Animator.StringToHash("State");
        private static readonly int CrawlingParameter = Animator.StringToHash("IsCrawling");
        private static readonly int CrouchingParameter = Animator.StringToHash("IsCrouching");
        private static readonly int DeathPostureParameter = Animator.StringToHash("DeathPosture");
        private static readonly int PosturePreviousParameter = Animator.StringToHash("PosturePrevious");

        #endregion


        #region Private Fields

        private int _previousPosture;

        #endregion


        #region Private Methods

        // 0 = standing, 1 = crouched, 2 = prone. Cover uses the crouch intent to pick
        // between the standing and crouched deaths.
        private static int ResolveDeathPosture(PlayerState state, bool isCrouching)
        {
            if (state == PlayerState.Crawling) return 2;
            if (state == PlayerState.Crouching || isCrouching) return 1;

            return 0;
        }
        private static readonly int WallHuggingParameter = Animator.StringToHash("IsWallHugging");
        private static readonly int KnockedOutParameter = Animator.StringToHash("IsKnockedOut");
        private static readonly int PeekParameter = Animator.StringToHash("Peek");
        private static readonly int CoverFacingParameter = Animator.StringToHash("CoverFacing");
        private static readonly int CoverTurnParameter = Animator.StringToHash("CoverTurn");

        #endregion
    }
}