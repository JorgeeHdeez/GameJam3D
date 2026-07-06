using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Detects outer corners on the player's left and right while in cover. From a
    /// point offset sideways along the wall, it probes back toward the wall: if the
    /// wall is no longer there, that side has ended, i.e. an outer corner the player
    /// can peek around.
    /// </summary>
    public sealed class CornerSensor : MonoBehaviour
    {
        #region Fields

        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private float _edgeProbeDistance = 0.6f;
        [SerializeField] private float _wallProbeDepth = 0.9f;
        [SerializeField] private float _originHeight = 1.0f;

        #endregion


        #region Properties

        /// <summary>True if the wall ends on the player's left (left outer corner).</summary>
        public bool HasLeftCorner => _hasLeftCorner;

        /// <summary>True if the wall ends on the player's right (right outer corner).</summary>
        public bool HasRightCorner => _hasRightCorner;

        #endregion


        #region Public API

        /// <summary>
        /// Updates the corner flags for the wall described by <paramref name="wallNormal"/>.
        /// </summary>
        public void Check(Transform origin, Vector3 wallNormal)
        {
            if (origin == null)
            {
                _hasLeftCorner = false;
                _hasRightCorner = false;
                return;
            }

            Vector3 basePosition = origin.position + Vector3.up * _originHeight;
            Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal);

            _hasRightCorner = IsEdge(basePosition + wallRight * _edgeProbeDistance, wallNormal);
            _hasLeftCorner = IsEdge(basePosition - wallRight * _edgeProbeDistance, wallNormal);
        }

        #endregion


        #region Private Fields

        private bool _hasLeftCorner;
        private bool _hasRightCorner;

        #endregion


        #region Private Methods

        // The probed side is an edge when the wall can no longer be found behind it.
        private bool IsEdge(Vector3 probeOrigin, Vector3 wallNormal) =>
            !Physics.Raycast(probeOrigin, -wallNormal, _wallProbeDepth, _wallLayer, QueryTriggerInteraction.Ignore);

        #endregion
    }
}