using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Player.Runtime;

namespace MenuUiControl.Editor
{
    [CustomEditor(typeof(WinTrigger))]
    public class WinTriggerEditor : UnityEditor.Editor
    {

        #region Unity Lifecycle

        private void OnEnable()
        {
            RefreshSceneNames();
        }

        #endregion


        #region Main

        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty("m_wonChannel"));

            EditorGUILayout.Space(4);

            var sceneNameProp = so.FindProperty("m_endSceneName");
            int currentIdx = _sceneNames.IndexOf(sceneNameProp.stringValue);
            if (currentIdx < 0) currentIdx = 0;

            int newIdx = EditorGUILayout.Popup("Scène de fin", currentIdx, _sceneNames.ToArray());
            if (newIdx != currentIdx)
                sceneNameProp.stringValue = _sceneNames[newIdx];

            so.ApplyModifiedProperties();
        }

        #endregion


        #region Private Methods

        private void RefreshSceneNames()
        {
            _sceneNames.Clear();

            var guids = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets/_/Scenes" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                _sceneNames.Add(Path.GetFileNameWithoutExtension(path));
            }
        }

        #endregion


        #region Private

        private List<string> _sceneNames = new();

        #endregion

    }
}
