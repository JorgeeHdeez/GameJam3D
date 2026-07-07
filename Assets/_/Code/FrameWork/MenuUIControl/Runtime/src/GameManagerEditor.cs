using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using MenuUiControl.Runtime;

namespace MenuUiControl.Editor
{
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : UnityEditor.Editor
    {

        #region Unity Lifecycle

        private void OnEnable()
        {
            RefreshSceneNames();
            RefreshButtons();
        }

        #endregion


        #region Main

        public override void OnInspectorGUI()
        {
            var gm = (GameManager)target;
            var so = serializedObject;
            so.Update();

            // Pause Action
            EditorGUILayout.PropertyField(so.FindProperty("m_pauseAction"));

            // Pause Scene — dropdown depuis Assets/_/Scenes
            EditorGUILayout.Space(4);
            var pauseSceneProp = so.FindProperty("m_pauseSceneName");
            int currentIdx = _sceneNames.IndexOf(pauseSceneProp.stringValue);
            if (currentIdx < 0) currentIdx = 0;
            int newIdx = EditorGUILayout.Popup("Pause Scene", currentIdx, _sceneNames.ToArray());
            if (newIdx != currentIdx)
                pauseSceneProp.stringValue = _sceneNames[newIdx];

            so.ApplyModifiedProperties();

            // Scene Buttons
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Scene Buttons", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (GUILayout.Button("⟳ Scanner les scènes ouvertes", GUILayout.Height(26)))
            {
                RefreshSceneNames();
                RefreshButtons();
            }

            EditorGUILayout.LabelField($"{_buttons.Count} bouton(s) trouvé(s)", EditorStyles.miniLabel);
            EditorGUILayout.Space(6);

            if (_buttons.Count == 0)
            {
                EditorGUILayout.HelpBox("Aucun SceneButton trouvé dans les scènes ouvertes.", MessageType.Info);
                return;
            }

            foreach (var btn in _buttons)
            {
                if (btn == null) continue;

                var btnSo = new SerializedObject(btn);
                btnSo.Update();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"{btn.gameObject.name}  —  {btn.gameObject.scene.name}",
                    EditorStyles.miniBoldLabel);
                if (GUILayout.Button("Select", GUILayout.Width(55)))
                    Selection.activeGameObject = btn.gameObject;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                EditorGUILayout.PropertyField(btnSo.FindProperty("m_action"));

                var actionProp = btnSo.FindProperty("m_action");
                var action = (SceneButtonAction)actionProp.enumValueIndex;

                if (action == SceneButtonAction.LoadScene)
                {
                    var sceneNameProp = btnSo.FindProperty("m_sceneName");
                    int idx = _sceneNames.IndexOf(sceneNameProp.stringValue);
                    if (idx < 0) idx = 0;
                    int nIdx = EditorGUILayout.Popup("Scène cible", idx, _sceneNames.ToArray());
                    if (nIdx != idx)
                        sceneNameProp.stringValue = _sceneNames[nIdx];

                    EditorGUILayout.PropertyField(btnSo.FindProperty("m_isOverlay"));
                    EditorGUILayout.PropertyField(btnSo.FindProperty("m_sortingOrder"));
                    EditorGUILayout.PropertyField(btnSo.FindProperty("m_setState"));
                }
                else if (action == SceneButtonAction.UnloadSelf)
                {
                    EditorGUILayout.PropertyField(btnSo.FindProperty("m_setState"));
                }

                if (btnSo.ApplyModifiedProperties())
                    EditorUtility.SetDirty(btn);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
        }

        #endregion


        #region Private Methods

        private void RefreshButtons()
        {
            _buttons.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                    _buttons.AddRange(root.GetComponentsInChildren<SceneButton>(true));
            }
        }

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

        private List<SceneButton> _buttons = new();
        private List<string> _sceneNames = new();

        #endregion

    }
}
