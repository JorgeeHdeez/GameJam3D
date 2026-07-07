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
            RefreshBuildScenes();
            RefreshButtons();
        }

        #endregion


        #region Main

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Scene Buttons", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (GUILayout.Button("⟳ Scanner les scènes ouvertes", GUILayout.Height(26)))
            {
                RefreshBuildScenes();
                RefreshButtons();
            }

            EditorGUILayout.LabelField(
                $"{_buttons.Count} bouton(s) trouvé(s)",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            if (_buttons.Count == 0)
            {
                EditorGUILayout.HelpBox("Aucun SceneButton trouvé dans les scènes ouvertes.", MessageType.Info);
                return;
            }

            foreach (var btn in _buttons)
            {
                if (btn == null) continue;

                var so = new SerializedObject(btn);
                so.Update();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"{btn.gameObject.name}  —  {btn.gameObject.scene.name}",
                    EditorStyles.miniBoldLabel);
                if (GUILayout.Button("Select", GUILayout.Width(55)))
                    Selection.activeGameObject = btn.gameObject;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                EditorGUILayout.PropertyField(so.FindProperty("m_action"));

                var actionProp = so.FindProperty("m_action");
                if ((SceneButtonAction)actionProp.enumValueIndex == SceneButtonAction.LoadScene)
                {
                    var sceneNameProp = so.FindProperty("m_sceneName");

                    int currentIdx = _buildSceneNames.IndexOf(sceneNameProp.stringValue);
                    if (currentIdx < 0) currentIdx = 0;

                    int newIdx = EditorGUILayout.Popup(
                        "Scène cible",
                        currentIdx,
                        _buildSceneNames.ToArray());

                    if (newIdx != currentIdx)
                        sceneNameProp.stringValue = _buildSceneNames[newIdx];

                    EditorGUILayout.PropertyField(so.FindProperty("m_isOverlay"));
                    EditorGUILayout.PropertyField(so.FindProperty("m_setState"));
                }

                if (so.ApplyModifiedProperties())
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

        private void RefreshBuildScenes()
        {
            _buildSceneNames.Clear();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(scene.path)) continue;
                _buildSceneNames.Add(Path.GetFileNameWithoutExtension(scene.path));
            }
        }

        #endregion


        #region Private

        private List<SceneButton> _buttons = new();
        private List<string> _buildSceneNames = new();

        #endregion

    }
}
