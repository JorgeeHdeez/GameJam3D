using Unity.Cinemachine;
using UnityEngine;
using MenuUiControl.Runtime;

namespace Player.Runtime
{
    [RequireComponent(typeof(CinemachineBrain))]
    public class CameraFreezeOnPause : MonoBehaviour
    {

        #region Unity Lifecycle

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            SetBrainEnabled(true);
        }

        private void Awake()
        {
            _brain = GetComponent<CinemachineBrain>();
        }

        #endregion


        #region Private Methods

        private void OnGameStateChanged(GameState state)
        {
            SetBrainEnabled(state != GameState.Paused);
        }

        private void SetBrainEnabled(bool enabled)
        {
            if (_brain != null)
                _brain.enabled = enabled;
        }

        #endregion


        #region Private

        private CinemachineBrain _brain;

        #endregion

    }
}
