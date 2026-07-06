using System;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// ScriptableObject event channel carrying a <see cref="NoisePing"/>. Decouples
    /// sound sources (e.g. the player knocking) from listeners (enemy perception and
    /// patrol) without direct references.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Noise Ping Event Channel", fileName = "NoisePingEventChannel")]
    public sealed class NoisePingEventChannelSO : ScriptableObject
    {
        #region Fields

        /// <summary>Raised every time <see cref="Raise"/> is called.</summary>
        public event Action<NoisePing> OnEventRaised;

        #endregion


        #region Public API

        /// <summary>Broadcasts <paramref name="value"/> to all current listeners.</summary>
        public void Raise(NoisePing value) => OnEventRaised?.Invoke(value);

        #endregion
    }
}