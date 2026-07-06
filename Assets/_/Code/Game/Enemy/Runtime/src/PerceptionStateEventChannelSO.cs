using System;
using UnityEngine;

namespace Enemy.Runtime
{
    /// <summary>
    /// ScriptableObject event channel carrying a <see cref="PerceptionState"/>.
    /// Lets UI, alarms or animation react to an enemy's awareness without a direct
    /// reference to the enemy.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Perception State Event Channel", fileName = "PerceptionStateEventChannel")]
    public sealed class PerceptionStateEventChannelSO : ScriptableObject
    {
        #region Fields

        /// <summary>Raised every time <see cref="Raise"/> is called.</summary>
        public event Action<PerceptionState> OnEventRaised;

        #endregion


        #region Public API

        /// <summary>Broadcasts <paramref name="value"/> to all current listeners.</summary>
        public void Raise(PerceptionState value) => OnEventRaised?.Invoke(value);

        #endregion
    }
}