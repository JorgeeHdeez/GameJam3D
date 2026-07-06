using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Detects a wall in a given direction by casting a fan of rays and aggregating
    /// the results. Using several rays makes detection robust for off-angle approaches
    /// and near corners, and averaging the hit normals yields a stable wall normal.
    /// The result is cached so the controller can read it during the same tick.
    /// </summary>
    public sealed class WallSensor : MonoBehaviour
    {
        #region Fields

        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private float _checkDistance = 0.6f;
        [SerializeField] private float _originHeight = 1.0f;
        [SerializeField] private int _rayCount = 6;
        [SerializeField] private float _arcAngle = 150.0f;

        #endregion


        #region Properties

        /// <summary>True if any ray hit a wall on the last <see cref="Check"/> call.</summary>
        public bool HasWall => _hasWall;

        /// <summary>Averaged surface normal of the detected wall (valid while <see cref="HasWall"/>).</summary>
        public Vector3 WallNormal => _wallNormal;

        #endregion


        #region Public API

        /// <summary>
        /// Casts a fan of rays centred on <paramref name="direction"/> and caches
        /// whether a wall was hit along with its averaged surface normal.
        /// </summary>
        public void Check(Transform origin, Vector3 direction)
        {
            _hasWall = false;
            _wallNormal = Vector3.zero;

            if (origin == null) return;
            if (direction.sqrMagnitude < DirectionEpsilon) return;

            Vector3 start = origin.position + Vector3.up * _originHeight;
            Vector3 forward = direction.normalized;
            int rays = Mathf.Max(1, _rayCount);

            Vector3 normalSum = Vector3.zero;
            int hitCount = 0;

            for (int i = 0; i < rays; i++)
            {
                float t = rays == 1 ? 0.5f : (float)i / (rays - 1);
                float angle = Mathf.Lerp(-_arcAngle * 0.5f, _arcAngle * 0.5f, t);
                Vector3 rayDirection = Quaternion.AngleAxis(angle, Vector3.up) * forward;

                if (!Physics.Raycast(start, rayDirection, out RaycastHit hit, _checkDistance, _wallLayer, QueryTriggerInteraction.Ignore)) continue;

                normalSum += hit.normal;
                hitCount++;
            }

            if (hitCount == 0) return;

            _hasWall = true;
            _wallNormal = (normalSum / hitCount).normalized;
        }

        #endregion


        #region Private Fields

        private bool _hasWall;
        private Vector3 _wallNormal;

        #endregion


        #region Private Constants

        private const float DirectionEpsilon = 0.0001f;

        #endregion
    }
}