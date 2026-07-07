using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// Shared flag telling perception systems whether the player is currently
    /// undetectable (e.g. tucked inside a hide spot). Injected as an asset so enemies
    /// can honour it without ever referencing the player, keeping the Enemy assembly
    /// independent of the Player one. Reset to visible by the hider on start, since a
    /// ScriptableObject can retain its value between play sessions in the editor.
    /// </summary>
    [CreateAssetMenu(menuName = "Player/Player Visibility", fileName = "PlayerVisibility")]
    public sealed class PlayerVisibilitySO : ScriptableObject
    {
        #region Properties

        /// <summary>True while the player cannot be seen or heard by enemies.</summary>
        public bool IsHidden => _isHidden;

        #endregion


        #region Public API

        public void SetHidden(bool value) => _isHidden = value;

        #endregion


        #region Private Fields

        private bool _isHidden;

        #endregion
    }
}
