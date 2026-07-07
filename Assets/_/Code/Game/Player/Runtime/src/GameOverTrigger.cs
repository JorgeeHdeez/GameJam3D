using System.Collections.Generic;
using Core.Runtime;
using MenuUiControl.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    public class GameOverTrigger : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private List<VoidEventChannelSO> m_catchChannels;

        #endregion


        #region Unity Lifecycle

        private void OnEnable()
        {
            foreach (var channel in m_catchChannels)
            {
                if (channel != null)
                    channel.OnEventRaised += TriggerGameOver;
            }
        }

        private void OnDisable()
        {
            foreach (var channel in m_catchChannels)
            {
                if (channel != null)
                    channel.OnEventRaised -= TriggerGameOver;
            }
        }

        #endregion


        #region Private Methods

        private void TriggerGameOver()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.SetState(GameState.GameOver);
            enabled = false;
        }

        #endregion

    }
}
