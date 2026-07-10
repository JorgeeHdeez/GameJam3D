using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    public class HeartbeatAudio : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private FloatEventChannelSO m_stressChannel;

        [Header("Respiration (0 → 1)")]
        [SerializeField] private AudioSource m_breathSource;
        [SerializeField] private float m_breathPitchMin = 0.8f;
        [SerializeField] private float m_breathPitchMax = 1.6f;
        [SerializeField] private float m_breathVolumeMin = 0.2f;
        [SerializeField] private float m_breathVolumeMax = 0.8f;

        [Header("Coeur lent (0.4 → 0.7)")]
        [SerializeField] private AudioSource m_heartSource1;
        [SerializeField] private float m_heart1PitchMin = 1.0f;
        [SerializeField] private float m_heart1PitchMax = 1.4f;
        [SerializeField] private float m_heart1VolumeMin = 0.1f;
        [SerializeField] private float m_heart1VolumeMax = 0.7f;

        [Header("Coeur rapide (0.6 → 1)")]
        [SerializeField] private AudioSource m_heartSource2;
        [SerializeField] private float m_heart2PitchMin = 1.2f;
        [SerializeField] private float m_heart2PitchMax = 1.8f;

        [Header("Crossfade")]
        [SerializeField] private float m_fadeSpeed = 2f;
        [SerializeField] private float m_pitchSpeed = 0.3f;

        #endregion


        #region Unity Lifecycle

        private void OnEnable()
        {
            if (m_stressChannel != null)
                m_stressChannel.OnEventRaised += OnStressChanged;

            StartAll();
        }

        private void OnDisable()
        {
            if (m_stressChannel != null)
                m_stressChannel.OnEventRaised -= OnStressChanged;
        }

        private void Update()
        {
            // Applique le nouveau pitch seulement quand le clip repart du début
            if (m_breathSource.isPlaying)
            {
                float normalizedTime = (float)m_breathSource.timeSamples / m_breathSource.clip.samples;
                if (normalizedTime < _lastBreathNormalizedTime)
                    m_breathSource.pitch = _targetBreathPitch;
                _lastBreathNormalizedTime = normalizedTime;
            }

            m_heartSource1.pitch = Mathf.MoveTowards(m_heartSource1.pitch, _targetHeart1Pitch, m_pitchSpeed * Time.deltaTime);
            m_heartSource2.pitch = Mathf.MoveTowards(m_heartSource2.pitch, _targetHeart2Pitch, m_pitchSpeed * Time.deltaTime);

            m_breathSource.volume  = Mathf.MoveTowards(m_breathSource.volume,  _targetBreathVolume,  m_fadeSpeed * Time.deltaTime);
            m_heartSource1.volume  = Mathf.MoveTowards(m_heartSource1.volume,  _targetHeart1Volume,  m_fadeSpeed * Time.deltaTime);
            m_heartSource2.volume  = Mathf.MoveTowards(m_heartSource2.volume,  _targetHeart2Volume,  m_fadeSpeed * Time.deltaTime);
        }

        #endregion


        #region Private Methods

        private void StartAll()
        {
            if (!m_breathSource.isPlaying)  m_breathSource.Play();
            if (!m_heartSource1.isPlaying)  m_heartSource1.Play();
            if (!m_heartSource2.isPlaying)  m_heartSource2.Play();

            m_breathSource.volume  = m_breathVolumeMin;
            m_heartSource1.volume  = 0f;
            m_heartSource2.volume  = 0f;

            _targetBreathPitch  = m_breathPitchMin;
            m_breathSource.pitch = m_breathPitchMin;
            _targetHeart1Pitch  = m_heart1PitchMin;
            _targetHeart2Pitch  = m_heart2PitchMin;
            _targetBreathVolume = m_breathVolumeMin;
        }

        private void OnStressChanged(float stress)
        {
            _targetBreathVolume = Mathf.Lerp(m_breathVolumeMin, m_breathVolumeMax, stress);
            _targetBreathPitch  = Mathf.Lerp(m_breathPitchMin, m_breathPitchMax, stress);

            float heart1Blend;
            if (stress < 0.2f)
                heart1Blend = 0f;
            else if (stress <= 0.6f)
                heart1Blend = (stress - 0.2f) / 0.2f;
            else if (stress <= 0.8f)
                heart1Blend = 1f - ((stress - 0.6f) / 0.2f);
            else
                heart1Blend = 0f;

            _targetHeart1Volume = stress < 0.2f ? 0f : Mathf.Lerp(m_heart1VolumeMax, 0f, stress < 0.6f ? 0f : (stress <= 0.8f ? (stress - 0.6f) / 0.2f : 1f));
            _targetHeart1Pitch  = Mathf.Lerp(m_heart1PitchMin, m_heart1PitchMax,
                stress < 0.2f ? 0f : (stress - 0.2f) / 0.8f);

            float heart2Blend = stress < 0.6f ? 0f : stress <= 0.8f ? (stress - 0.6f) / 0.2f : 1f;
            _targetHeart2Volume = heart2Blend;
            _targetHeart2Pitch  = Mathf.Lerp(m_heart2PitchMin, m_heart2PitchMax,
                stress < 0.6f ? 0f : (stress - 0.6f) / 0.4f);
        }

        #endregion


        #region Private

        private float _targetBreathVolume;
        private float _targetBreathPitch;
        private float _targetHeart1Volume;
        private float _targetHeart1Pitch;
        private float _targetHeart2Volume;
        private float _targetHeart2Pitch;
        private float _lastBreathNormalizedTime;

        #endregion

    }
}
