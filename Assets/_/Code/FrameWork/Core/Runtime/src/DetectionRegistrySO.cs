using System.Collections.Generic;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// Runtime registry of active detection sources (typically enemy perceptions).
    /// Sources register on enable and unregister on disable, exactly like the update
    /// loop, so aggregators can read the current maximum awareness without holding a
    /// direct reference to any enemy or crossing assembly boundaries. Injected as an
    /// asset, so it is shared state on an asset rather than static mutable state.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Detection Registry", fileName = "DetectionRegistry")]
    public sealed class DetectionRegistrySO : ScriptableObject
    {
        #region Properties

        /// <summary>Highest detection level (0..1) across all registered sources.</summary>
        public float MaxDetectionLevel
        {
            get
            {
                float max = 0.0f;

                for (int i = 0; i < _sources.Count; i++)
                {
                    float level = _sources[i].DetectionLevel;
                    if (level > max) max = level;
                }

                return max;
            }
        }

        #endregion


        #region Public API

        public void Register(IDetectionSource source)
        {
            if (source == null) return;
            if (_sources.Contains(source)) return;

            _sources.Add(source);
        }

        public void Unregister(IDetectionSource source) => _sources.Remove(source);

        #endregion


        #region Private Fields

        private readonly List<IDetectionSource> _sources = new();

        #endregion
    }
}
