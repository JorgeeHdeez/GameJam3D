using UnityEngine;
using PrimeTween;

namespace Interractions.Runtime
{
    public class Interractors : MonoBehaviour
    {
        [Header("Porte")]
        [SerializeField] private Transform targetDoor;
        [SerializeField] private Vector3 openRotation = new Vector3(0, 90, 0);
        [SerializeField] private float doorDuration = 0.5f;

        [Header("Objet Feedback")]
        [SerializeField] private Transform itemVisual;
        [SerializeField] private float popDuration = 0.3f;

        [Header("Audio Fade")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float fadeDuration = 1f;

        // 1. ANIME LA PORTE (Rebond � la fin avec Ease.OutBack)
        public void OpenDoor()
        {
            Tween.LocalRotation(targetDoor, openRotation, doorDuration, Ease.OutBack);
        }

        // 2. EFFET POP (Loot ou interaction)
        public void PopItem()
        {
            itemVisual.localScale = Vector3.zero;
            // Monte � 1.2 puis se stabilise � 1.0 gr�ce � OutBack
            Tween.Scale(itemVisual, 1f, popDuration, Ease.OutBack);
        }

        // 3. FADE AUDIO (Id�al pour changer d'ambiance en douceur)
        public void FadeOutAudio()
        {
            Tween.AudioVolume(audioSource, 0f, fadeDuration, Ease.Linear)
                 .OnComplete(() => audioSource.Stop());
        }

        public void FadeInAudio(AudioClip newClip)
        {
            audioSource.clip = newClip;
            audioSource.volume = 0f;
            audioSource.Play();
            Tween.AudioVolume(audioSource, 1f, fadeDuration, Ease.Linear);
        }
    }
}