using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Interractions.Runtime
{
    public class OpenDoors : MonoBehaviour
    {

        #region Inspector

        [Header("Configuration")]
        [SerializeField] private Vector3 m_openRotation = new Vector3(0, 90, 0);
        [SerializeField] private float m_doorDuration = 3.5f;

        [Header("Interaction")]
        [SerializeField] private Transform m_playerTransform;
        [SerializeField] private float m_maxInteractionDistance = 5f;

        #endregion


        #region Unity Lifecycle

        private void Start()
        {
            _closedRotation = transform.localEulerAngles;
            _closedPosition = transform.localPosition;

            _mainCamera = Camera.main;
            if (_mainCamera == null)
                Debug.LogError("[OPEN DOORS]: Caméra principale introuvable.");

            if (m_playerTransform == null)
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null)
                    m_playerTransform = playerGO.transform;
                else
                    Debug.LogError("[OPEN DOORS]: Aucun GameObject avec le tag 'Player'.");
            }
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || _mainCamera == null) return;

            var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                TriggerInteraction();
        }

        #endregion


        #region Main

        public void TriggerInteraction()
        {
            if (m_playerTransform == null) return;

            float distance = Vector3.Distance(transform.position, m_playerTransform.position);
            if (distance > m_maxInteractionDistance)
            {
                Debug.LogWarning("[OPEN DOORS]: Trop loin !");
                return;
            }

            if (_isOpen)
                CloseDoor();
            else
                OpenDoor();
        }

        public void OpenDoor()
        {
            _isOpen = true;

            Sequence.Create()
                .Group(Tween.LocalRotation(transform, m_openRotation, m_doorDuration, Ease.OutBack))
                .Group(Tween.LocalPositionX(transform, _closedPosition.x - 0.039149f, m_doorDuration / 3.5f, Ease.InOutSine))
                .Group(Tween.LocalPositionZ(transform, _closedPosition.z + 0.80711f, m_doorDuration / 3.5f, Ease.InOutSine));
        }

        public void CloseDoor()
        {
            _isOpen = false;

            Sequence.Create()
                .Group(Tween.LocalRotation(transform, _closedRotation, m_doorDuration, Ease.InBack))
                .Group(Tween.LocalPositionX(transform, _closedPosition.x, m_doorDuration / 3.5f, Ease.InOutSine))
                .Group(Tween.LocalPositionZ(transform, _closedPosition.z, m_doorDuration / 3.5f, Ease.InOutSine));
        }

        #endregion


        #region Private

        private Camera _mainCamera;
        private bool _isOpen = false;
        private Vector3 _closedRotation;
        private Vector3 _closedPosition;

        #endregion

    }
}
