using System.Collections.Generic;
using UnityEngine;

namespace MenuUiControl.Runtime
{
    [CreateAssetMenu(fileName = "UIManagerConfig", menuName = "FrameWork/UI Manager Config")]
    public class UIManagerConfig : ScriptableObject
    {

        #region Private

        [HideInInspector] public List<UIScreen> m_screens = new List<UIScreen>();
        [HideInInspector] public SceneReference m_managerScene = new SceneReference();

        #endregion

    }
}
