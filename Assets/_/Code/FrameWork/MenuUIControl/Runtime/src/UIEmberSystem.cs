using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MenuUiControl.Runtime
{
    public class UIEmberSystem : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private RectTransform m_spawnArea;
        [SerializeField] private Sprite m_emberSprite;
        [SerializeField] private int m_maxEmbers = 20;
        [SerializeField] private float m_spawnRate = 0.2f;
        [SerializeField] private float m_riseHeight = 300f;
        [SerializeField] private float m_lifetime = 2f;
        [SerializeField] private float m_minSize = 4f;
        [SerializeField] private float m_maxSize = 10f;
        [SerializeField] private Color m_colorStart = new Color(1f, 0.3f, 0f, 1f);
        [SerializeField] private Color m_colorEnd = new Color(1f, 0.1f, 0f, 0f);

        #endregion


        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(SpawnLoop());
        }

        #endregion


        #region Private Methods

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(m_spawnRate);
                if (_activeEmbers < m_maxEmbers)
                    StartCoroutine(SpawnEmber());
            }
        }

        private IEnumerator SpawnEmber()
        {
            _activeEmbers++;
            var go = new GameObject("Ember");
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            if (m_emberSprite != null)
                img.sprite = m_emberSprite;

            float size = Random.Range(m_minSize, m_maxSize);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            float spawnWidth = m_spawnArea != null ? m_spawnArea.rect.width : 500f;
            rt.anchoredPosition = new Vector2(Random.Range(-spawnWidth / 2f, spawnWidth / 2f), 0f);

            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;
            float drift = Random.Range(-30f, 30f);

            while (elapsed < m_lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_lifetime;

                rt.anchoredPosition = new Vector2(
                    startPos.x + Mathf.Sin(elapsed * 2f) * drift,
                    startPos.y + t * m_riseHeight
                );

                float alpha = t < 0.5f ? 1f : 1f - ((t - 0.5f) / 0.5f);
                img.color = Color.Lerp(m_colorStart, m_colorEnd, t) * new Color(1, 1, 1, alpha);

                float scale = Mathf.Lerp(1f, 0.2f, t);
                rt.localScale = Vector3.one * scale;

                yield return null;
            }

            _activeEmbers--;
            Destroy(go);
        }

        #endregion


        #region Private

        private int _activeEmbers = 0;

        #endregion

    }
}
