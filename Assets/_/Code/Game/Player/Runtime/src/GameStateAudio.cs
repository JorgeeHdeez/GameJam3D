using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    public class GameStateAudio : MonoBehaviour
    {

        #region Inspector

        [Header("Rewind")]
        [SerializeField] private GameStateSO m_gameState;
        [SerializeField] private AudioSource m_rewindSource;
        [SerializeField] private AudioClip[] m_rewindClips;

        [Header("Repéré")]
        [SerializeField] private VoidEventChannelSO m_alarmChannel;
        [SerializeField] private AudioSource m_alarmSource;
        [SerializeField] private AudioClip m_alarmClip;
        [SerializeField] private float m_alarmCooldown = 3f;

        [Header("Victoire")]
        [SerializeField] private VoidEventChannelSO m_wonChannel;
        [SerializeField] private AudioSource m_wonSource;
        [SerializeField] private AudioClip m_wonClip;

        #endregion


        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _alarmCooldownTimer = m_alarmCooldown;
        }

        private void OnEnable()
        {
            if (m_gameState != null)
                m_gameState.OnStateChanged += OnStateChanged;

            if (m_alarmChannel != null)
                m_alarmChannel.OnEventRaised += OnAlarm;

            if (m_wonChannel != null)
                m_wonChannel.OnEventRaised += OnWon;
        }

        private void OnDisable()
        {
            if (m_gameState != null)
                m_gameState.OnStateChanged -= OnStateChanged;

            if (m_alarmChannel != null)
                m_alarmChannel.OnEventRaised -= OnAlarm;

            if (m_wonChannel != null)
                m_wonChannel.OnEventRaised -= OnWon;
        }

        private void Update()
        {
            if (_alarmCooldownTimer > 0f)
                _alarmCooldownTimer -= Time.deltaTime;
        }

        #endregion


        #region Private Methods

        private void OnStateChanged(GameStatePlayer state)
        {
            if (state == GameStatePlayer.Rewinding)
            {
                AudioListener.volume = 0f;

                if (m_rewindClips == null || m_rewindClips.Length == 0) return;
                int idx = Random.Range(0, m_rewindClips.Length);
                m_rewindSource.clip = m_rewindClips[idx];
                m_rewindSource.volume = 1f;
                m_rewindSource.ignoreListenerVolume = true;
                m_rewindSource.Play();
            }
            else if (state == GameStatePlayer.Playing)
            {
                AudioListener.volume = 1f;
                _alarmCooldownTimer = 0f;
            }
        }

        private void OnAlarm()
        {
            if (_alarmCooldownTimer > 0f) return;
            if (m_alarmSource == null || m_alarmClip == null) return;

            m_alarmSource.clip = m_alarmClip;
            m_alarmSource.Play();
            _alarmCooldownTimer = m_alarmCooldown;
        }

        private void OnWon()
        {
            AudioListener.volume = 0f;
            DontDestroyOnLoad(gameObject);
            if (m_wonSource == null || m_wonClip == null) return;
            m_wonSource.ignoreListenerVolume = true;
            m_wonSource.clip = m_wonClip;
            m_wonSource.Play();
        }

        #endregion


        #region Private

        private float _alarmCooldownTimer;
        private static GameStateAudio _instance;

        #endregion

    }
}
