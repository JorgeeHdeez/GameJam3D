using UnityEngine;

namespace MenuUiControl.Runtime
{
    [System.Serializable]
    public class SceneReference
    {

        #region Inspector

#if UNITY_EDITOR
        public UnityEditor.SceneAsset m_sceneAsset;
#endif

        #endregion


        #region Properties

        public string ScenePath => _scenePath;
        public string SceneName => _sceneName;
        public string SourceGuid => _sourceGuid;
        public string ReturnName => _returnName;

        #endregion


        #region Main

        public void Sync()
        {
#if UNITY_EDITOR
            _scenePath = m_sceneAsset
                ? UnityEditor.AssetDatabase.GetAssetPath(m_sceneAsset)
                : string.Empty;
            _sceneName = m_sceneAsset
                ? m_sceneAsset.name
                : string.Empty;
#endif
        }

        public void SetSourceGuid(string guid) => _sourceGuid = guid;

        public void SetReturnName(string returnName) => _returnName = returnName;

        #endregion


        #region Private

        [SerializeField, HideInInspector] private string _scenePath;
        [SerializeField, HideInInspector] private string _sceneName;
        [SerializeField, HideInInspector] private string _sourceGuid;
        [SerializeField, HideInInspector] private string _returnName;

        #endregion

    }
}
