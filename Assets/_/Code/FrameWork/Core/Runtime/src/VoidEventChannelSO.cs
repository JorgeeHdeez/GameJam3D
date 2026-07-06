using System;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// ScriptableObject event channel that carries no payload. Used for one-shot
    /// signals such as raising an alarm, decoupling the sender from the receiver.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Void Event Channel", fileName = "VoidEventChannel")]
    public sealed class VoidEventChannelSO : ScriptableObject
    {
        #region Fields

        /// <summary>Raised every time <see cref="Raise"/> is called.</summary>
        public event Action OnEventRaised;

        #endregion


        #region Public API

        /// <summary>Notifies all current listeners.</summary>
        public void Raise() => OnEventRaised?.Invoke();

        #endregion
    }
}