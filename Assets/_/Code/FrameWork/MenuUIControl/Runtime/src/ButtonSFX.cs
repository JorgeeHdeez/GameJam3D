using UnityEngine;
using UnityEngine.EventSystems;

namespace MenuUiControl.Runtime
{
    public class ButtonSFX : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {

        #region Inspector

        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioClip m_hoverSound;

        #endregion


        #region Main

        public void OnPointerEnter(PointerEventData eventData) => PlaySound();
        public void OnSelect(BaseEventData eventData) => PlaySound();

        #endregion


        #region Private Methods

        private void PlaySound()
        {
            if (m_audioSource && m_hoverSound)
                m_audioSource.PlayOneShot(m_hoverSound);
        }

        #endregion

    }
}
