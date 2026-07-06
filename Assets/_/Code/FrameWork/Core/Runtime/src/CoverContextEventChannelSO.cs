using System;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// ScriptableObject event channel carrying a <see cref="CoverContext"/> payload.
    /// Decouples the player (emitter) from the cover camera rig (listener).
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Cover Context Event Channel", fileName = "CoverContextEventChannel")]
    public sealed class CoverContextEventChannelSO : ScriptableObject
    {
        #region Fields

        /// <summary>Raised every time <see cref="Raise"/> is called.</summary>
        public event Action<CoverContext> OnEventRaised;

        #endregion


        #region Public API

        /// <summary>Broadcasts <paramref name="value"/> to all current listeners.</summary>
        public void Raise(CoverContext value) => OnEventRaised?.Invoke(value);

        #endregion
    }
}