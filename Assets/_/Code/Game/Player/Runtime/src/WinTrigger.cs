using Core.Runtime;
using MenuUiControl.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player.Runtime
{
    public class WinTrigger : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private VoidEventChannelSO m_wonChannel;
        [SerializeField] private string m_endSceneName;

        #endregion


        #region Unity Lifecycle

        private void OnEnable()
        {
            if (m_wonChannel != null)
                m_wonChannel.OnEventRaised += OnWon;
        }

        private void OnDisable()
        {
            if (m_wonChannel != null)
                m_wonChannel.OnEventRaised -= OnWon;
        }

        #endregion


        #region Private Methods

        private void OnWon()
        {
            if (string.IsNullOrEmpty(m_endSceneName)) return;
            SceneManager.LoadSceneAsync(m_endSceneName, LoadSceneMode.Single);
        }

        #endregion

    }
}
