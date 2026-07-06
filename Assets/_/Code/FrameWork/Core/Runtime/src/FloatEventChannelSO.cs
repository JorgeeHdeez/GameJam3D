using System;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// ScriptableObject event channel carrying a single float payload. Used to
    /// decouple emitters (e.g. player noise) from listeners (e.g. enemy perception)
    /// without any direct reference between the two systems.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Float Event Channel", fileName = "FloatEventChannel")]
    public sealed class FloatEventChannelSO : ScriptableObject
    {
        #region Fields

        /// <summary>Raised every time <see cref="Raise"/> is called.</summary>
        public event Action<float> OnEventRaised;

        #endregion


        #region Public API

        /// <summary>Broadcasts <paramref name="value"/> to all current listeners.</summary>
        public void Raise(float value) => OnEventRaised?.Invoke(value);

        #endregion
    }
}