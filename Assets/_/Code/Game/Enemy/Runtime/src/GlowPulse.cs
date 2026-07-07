using UnityEngine;

namespace Enemy.Runtime
{
    public class GlowPulse : MonoBehaviour
    {
        public Color emissionColor = Color.red;
        public float minIntensity = 1.5f;
        public float maxIntensity = 3.5f;
        public float speed = 2f;

        private Material mat;

        void Start()
        {
            mat = GetComponent<Renderer>().material;
            mat.EnableKeyword("_EMISSION");
        }

        void Update()
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            mat.SetColor("_EmissionColor", emissionColor * intensity);
        }
    }
}
