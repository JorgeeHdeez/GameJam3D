using UnityEngine;
using UnityEngine.InputSystem;
using PrimeTween;

namespace Interractions.Runtime
{
    public class OpenDoors : MonoBehaviour
    {
        #region Inspecteur

        [Header("Configuration")]
        [SerializeField] private Vector3 openRotation = new Vector3(0, 90, 0);
        [SerializeField] private float doorDuration = 0.5f;

        [Header("Interaction")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float maxInteractionDistance = 5f;

        #endregion

        private Camera mainCamera; 
        private bool isOpen = false;

        private void Start()
        {
            Debug.Log($"<color=cyan>[PORTAL CRITICAL]</color> Start() commencé sur {gameObject.name}.");

            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("<color=red>[PORTAL ERROR]</color> Caméra principale ('MainCamera') introuvable.");
            }

            if (playerTransform == null)
            {
                GameObject playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null)
                {
                    playerTransform = playerGO.transform;
                    Debug.Log("<color=green>[PORTAL INFO]</color> Joueur assigné avec succès !");
                }
                else
                {
                    Debug.LogError("<color=red>[PORTAL ERROR]</color> Aucun GameObject avec le tag 'Player' dans la scène !");
                }
            }

            Debug.Log($"<color=cyan>[PORTAL CRITICAL]</color> Start() terminé proprement sur {gameObject.name}.");
        }

        private void Update()
        {
            // Check si la souris existe et si le clic gauche vient d'être enfoncé
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && mainCamera != null)
            {
                // On récupère la position de la souris via le New Input System
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        Debug.Log($"<color=yellow>[PORTAL CLICK]</color> Clic détecté sur : {hit.transform.name}");
                        TriggerInteraction();
                    }
                }
            }
        }

        public void TriggerInteraction()
        {
            if (isOpen) return;

            if (playerTransform == null)
            {
                Debug.LogError("<color=red>[PORTAL ERROR]</color> playerTransform est NULL.");
                return;
            }

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            Debug.Log($"<color=orange>[PORTAL DISTANCE]</color> Distance : {distance}m / Max : {maxInteractionDistance}m");

            if (distance <= maxInteractionDistance)
            {
                OpenDoor();
            }
            else
            {
                Debug.LogWarning("<color=orange>[PORTAL DISTANCE]</color> Trop loin !");
            }
        }

        public void OpenDoor()
        {
            isOpen = true;
            Debug.Log($"<color=green>[PORTAL TWEEN]</color> Lancement de la séquence sur {gameObject.name}");

            Vector3 currentLocalPos = transform.localPosition;

            Sequence.Create()
                .Group(Tween.LocalRotation(transform, openRotation, doorDuration, Ease.OutBack))
                .Group(Tween.LocalPositionX(transform, currentLocalPos.x - 0.5f, doorDuration / 3.5f, Ease.InOutSine))
                .Group(Tween.LocalPositionZ(transform, currentLocalPos.z - 0.5f, doorDuration / 3.5f, Ease.InOutSine))

                .OnComplete(() => Debug.Log("<color=green>[PORTAL TWEEN]</color> Tous les mouvements sont finis !"));
        }
    }
}