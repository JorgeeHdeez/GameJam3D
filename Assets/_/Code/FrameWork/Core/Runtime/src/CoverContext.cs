using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// Immutable snapshot of the player's cover/peek situation, broadcast to the
    /// camera so it can frame around corners without referencing the player directly.
    /// </summary>
    public readonly struct CoverContext
    {
        #region Properties

        /// <summary>True while the player is in wall-hug cover.</summary>
        public bool IsInCover { get; }

        /// <summary>World position the cover camera should sit at.</summary>
        public Vector3 CameraPosition { get; }

        /// <summary>World point the cover camera should look at (slides past the
        /// corner while peeking).</summary>
        public Vector3 LookAt { get; }

        #endregion


        #region Public API

        public CoverContext(bool isInCover, Vector3 cameraPosition, Vector3 lookAt)
        {
            IsInCover = isInCover;
            CameraPosition = cameraPosition;
            LookAt = lookAt;
        }

        #endregion
    }
}