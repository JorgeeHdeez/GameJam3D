namespace Player.Runtime
{
    /// <summary>
    /// Pure decision logic that resolves the next <see cref="PlayerState"/> from
    /// the current frame's context. Contains no Unity dependencies, which keeps it
    /// trivially testable and free of side effects.
    /// </summary>
    public sealed class PlayerStateMachine
    {
        #region Public API

        /// <summary>
        /// Resolves the active state. Priority, from highest to lowest: knocked out,
        /// cover (wall hugging), crawling, crouching, then standing locomotion
        /// (idle / walk / run). Crouching covers both crouched idle and crouched
        /// movement; the blend tree picks idle vs walk from the speed parameter.
        /// </summary>
        public PlayerState Evaluate(bool isKnockedOut, bool wantsCover, bool hasWall, bool isCrawling, bool isCrouching, bool isMoving, bool wantsRun)
        {
            if (isKnockedOut) return PlayerState.KnockedOut;
            if (wantsCover && hasWall) return PlayerState.WallHugging;
            if (isCrawling) return PlayerState.Crawling;
            if (isCrouching) return PlayerState.Crouching;
            if (!isMoving) return PlayerState.Idle;

            return wantsRun ? PlayerState.Running : PlayerState.Walking;
        }

        #endregion
    }
}