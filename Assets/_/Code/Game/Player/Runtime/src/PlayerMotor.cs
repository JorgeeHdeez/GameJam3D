using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Applies translation, gravity and rotation through an injected
    /// <see cref="CharacterController"/>. Holds no game logic: it only executes
    /// the movement commands issued by the <see cref="PlayerController"/>.
    /// </summary>
    public sealed class PlayerMotor : MonoBehaviour
    {
        #region Fields

        [SerializeField] private CharacterController _characterController;
        [SerializeField] private float _gravity = -20.0f;
        [SerializeField] private float _turnSpeed = 720.0f;

        #endregion


        #region Properties

        /// <summary>True while the character controller reports ground contact.</summary>
        public bool IsGrounded => _characterController.isGrounded;

        #endregion


        #region Public API

        /// <summary>
        /// Moves the character along <paramref name="worldDirection"/> at the given
        /// planar speed, integrating gravity on the vertical axis.
        /// </summary>
        public void Move(Vector3 worldDirection, float speed, float deltaTime)
        {
            if (IsGrounded && _verticalVelocity < 0.0f) _verticalVelocity = StickToGroundForce;

            _verticalVelocity += _gravity * deltaTime;

            Vector3 planar = worldDirection * speed;
            Vector3 displacement = new(planar.x, _verticalVelocity, planar.z);
            _characterController.Move(displacement * deltaTime);
        }

        /// <summary>Rotates the character to face <paramref name="faceDirection"/>.</summary>
        public void FaceTowards(Vector3 faceDirection, float deltaTime)
        {
            if (faceDirection.sqrMagnitude < DirectionEpsilon) return;

            Quaternion target = Quaternion.LookRotation(faceDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _turnSpeed * deltaTime);
        }

        #endregion


        #region Private Fields

        private float _verticalVelocity;

        #endregion


        #region Private Constants

        private const float DirectionEpsilon = 0.0001f;
        private const float StickToGroundForce = -1.0f;

        #endregion
    }
}