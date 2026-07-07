using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// A safe spot the player can tuck into (a cabinet, under a table). While hidden
    /// here the player is undetectable and the camera is framed from
    /// <see cref="CameraAnchor"/>. Needs a trigger collider covering the approach: the
    /// spot offers itself to the player while they stand inside it. Position the
    /// camera anchor by hand for whatever framing reads best (inside the cabinet,
    /// under the table, ...).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class HideSpot : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Transform _cameraAnchor;

        #endregion


        #region Properties

        /// <summary>Pose the camera snaps to while hidden in this spot.</summary>
        public Transform CameraAnchor => _cameraAnchor;

        #endregion


        #region Unity Callbacks

        private void OnTriggerEnter(Collider other)
        {
            PlayerHideController hider = other.GetComponentInParent<PlayerHideController>();
            if (hider == null) return;

            hider.SetAvailableSpot(this);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerHideController hider = other.GetComponentInParent<PlayerHideController>();
            if (hider == null) return;

            hider.ClearAvailableSpot(this);
        }

        #endregion
    }
}
