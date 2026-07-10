using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Full view pose captured for one recorded frame: the body transform plus the
    /// camera pivot's local pose, so replaying it reconstructs both where the player
    /// stood and exactly where they were looking (including crouch/crawl height and
    /// cover framing).
    /// </summary>
    public struct PlayerSnapshot
    {
        #region Fields

        public Vector3 BodyPosition;
        public Quaternion BodyRotation;
        public Vector3 PivotLocalPosition;
        public Quaternion PivotLocalRotation;

        #endregion


        #region Public API

        public PlayerSnapshot(Vector3 bodyPosition, Quaternion bodyRotation, Vector3 pivotLocalPosition, Quaternion pivotLocalRotation)
        {
            BodyPosition = bodyPosition;
            BodyRotation = bodyRotation;
            PivotLocalPosition = pivotLocalPosition;
            PivotLocalRotation = pivotLocalRotation;
        }

        #endregion
    }
}
