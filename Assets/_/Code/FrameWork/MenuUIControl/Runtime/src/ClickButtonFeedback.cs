using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MenuUiControl.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ClickButtonFeedback : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {

        #region Inspector

        [SerializeField, Range(0.5f, 0.99f)] private float m_pressedScale = 0.88f;
        [SerializeField, Range(1.0f, 1.5f)] private float m_hoverScale = 1.08f;
        [SerializeField] private float m_pressDuration = 0.06f;
        [SerializeField] private float m_releaseDuration = 0.14f;
        [SerializeField] private float m_hoverDuration = 0.1f;

        #endregion


        #region Unity Lifecycle

        private void Awake() => _normalScale = transform.localScale;

        private void OnDisable()
        {
            if (_anim != null)
            {
                StopCoroutine(_anim);
                _anim = null;
            }
            transform.localScale = _normalScale;
        }

        #endregion


        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData) => Animate(_normalScale * m_pressedScale, m_pressDuration);
        public void OnPointerUp(PointerEventData eventData) => Animate(_normalScale, m_releaseDuration);
        public void OnPointerEnter(PointerEventData eventData) => Animate(_normalScale * m_hoverScale, m_hoverDuration);
        public void OnPointerExit(PointerEventData eventData) => Animate(_normalScale, m_hoverDuration);

        #endregion


        #region Private Methods

        private void Animate(Vector3 target, float duration)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(ScaleTo(target, duration));
        }

        private IEnumerator ScaleTo(Vector3 target, float duration)
        {
            Vector3 from = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.localScale = Vector3.Lerp(from, target, t);
                yield return null;
            }
            transform.localScale = target;
            _anim = null;
        }

        #endregion


        #region Private

        private Vector3 _normalScale;
        private Coroutine _anim;

        #endregion

    }
}
