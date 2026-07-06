using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// One-shot positioned sound (e.g. a knock on a wall) that any listener within its
    /// radius can hear and react to.
    /// </summary>
    public readonly struct NoisePing
    {
        #region Properties

        /// <summary>World position the sound originates from.</summary>
        public Vector3 Position { get; }

        /// <summary>How far the sound carries.</summary>
        public float Radius { get; }

        #endregion


        #region Public API

        public NoisePing(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        #endregion
    }
}