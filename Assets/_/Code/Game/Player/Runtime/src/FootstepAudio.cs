using UnityEngine;

namespace Player.Runtime
{
    public class FootstepAudio : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private PlayerController m_playerController;
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioClip[] m_footstepClips;

        [Header("Cadence (secondes entre chaque pas)")]
        [SerializeField] private float m_runInterval = 0.35f;
        [SerializeField] private float m_crouchInterval = 0.55f;
        [SerializeField] private float m_crawlInterval = 0.75f;
        [SerializeField] private float m_wallHugInterval = 0.6f;

        #endregion


        #region Unity Lifecycle

        private void Update()
        {
            if (m_playerController == null) return;

            var state = m_playerController.CurrentState;

            if (state == PlayerState.Idle || state == PlayerState.KnockedOut)
            {
                _timer = 0f;
                return;
            }

            float interval = ResolveInterval(state);
            _timer += Time.deltaTime;

            if (_timer >= interval)
            {
                _timer = 0f;
                PlayRandom();
            }
        }

        #endregion


        #region Private Methods

        private float ResolveInterval(PlayerState state) => state switch
        {
            PlayerState.Crouching   => m_crouchInterval,
            PlayerState.Crawling    => m_crawlInterval,
            PlayerState.WallHugging => m_wallHugInterval,
            _                       => m_runInterval
        };

        private void PlayRandom()
        {
            if (m_footstepClips == null || m_footstepClips.Length == 0) return;
            if (m_audioSource == null) return;

            int idx = Random.Range(0, m_footstepClips.Length);
            m_audioSource.PlayOneShot(m_footstepClips[idx]);
        }

        #endregion


        #region Private

        private float _timer;

        #endregion

    }
}
