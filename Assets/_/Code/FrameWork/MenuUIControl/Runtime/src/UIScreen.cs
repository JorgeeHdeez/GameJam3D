using UnityEngine;

namespace MenuUiControl.Runtime
{
    [CreateAssetMenu(fileName = "UIScreen", menuName = "FrameWork/UI Screen")]
    public class UIScreen : ScriptableObject
    {

        #region Unity Lifecycle

        private void OnValidate()
        {
#if UNITY_EDITOR
            m_scene?.Sync();
#endif
        }

        #endregion


        #region Private

        [HideInInspector] public SceneReference m_scene = new SceneReference();
        [HideInInspector] public bool m_loadOnStart = false;
        [HideInInspector] public UITrigger m_showTrigger = UITrigger.None;
        [HideInInspector] public UIHideTrigger m_hideTrigger = UIHideTrigger.None;
        [HideInInspector] public bool m_isOverlay = false;
        [HideInInspector] public bool m_blockPause = false;

        #endregion

    }
}
