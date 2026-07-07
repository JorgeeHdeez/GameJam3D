using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using MenuUiControl.Runtime;

namespace MenuUiControl.Editor
{
    public class SceneButtonConfigurator : EditorWindow
    {

        #region Menu

        [MenuItem("Managers/Scene Button Configurator")]
        public static void Open()
        {
            var window = GetWindow<SceneButtonConfigurator>("Scene Buttons");
            window.minSize = new Vector2(400, 300);
        }

        #endregion


        #region GUI

        private void OnEnable()
        {
            RefreshBuildScenes();
            RefreshButtons();
        }

        private void OnFocus()
        {
            RefreshBuildScenes();
            RefreshButtons();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawToolbar();
            DrawSeparator();
            DrawButtons();

            EditorGUILayout.EndScrollView();
        }

        #endregion


        #region Draw

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("⟳ Scanner les scènes ouvertes", GUILayout.Height(26)))
                RefreshButtons();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                $"{_buttons.Count} bouton(s) trouvé(s) dans {SceneManager.sceneCount} scène(s) ouverte(s)",
                EditorStyles.miniLabel);
        }

        private void DrawButtons()
        {
            if (_buttons.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Aucun SceneButton trouvé. Ouvre une scène avec des boutons.",
                    MessageType.Info);
                return;
            }

            foreach (var btn in _buttons)
            {
                if (btn == null) continue;

                var so = new SerializedObject(btn);
                so.Update();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"{btn.gameObject.name}  —  {btn.gameObject.scene.name}",
                    EditorStyles.miniBoldLabel);
                if (GUILayout.Button("Select", GUILayout.Width(55)))
                    Selection.activeGameObject = btn.gameObject;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                // Action
                EditorGUILayout.PropertyField(so.FindProperty("m_action"));

                // Scene name — dropdown des scènes du Build Profile
                var actionProp = so.FindProperty("m_action");
                var sceneAction = (SceneButtonAction)actionProp.enumValueIndex;

                if (sceneAction == SceneButtonAction.LoadScene)
                {
                    var sceneNameProp = so.FindProperty("m_sceneName");
                    int currentIdx = _buildSceneNames.IndexOf(sceneNameProp.stringValue);
                    if (currentIdx < 0) currentIdx = 0;

                    int newIdx = EditorGUILayout.Popup(
                        "Scene cible",
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

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(4);
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
                {
                    var found = root.GetComponentsInChildren<SceneButton>(true);
                    _buttons.AddRange(found);
                }
            }
        }

        private void RefreshBuildScenes()
        {
            _buildSceneNames.Clear();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(scene.path)) continue;
                var name = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                _buildSceneNames.Add(name);
            }
        }

        #endregion


        #region Private

        private Vector2 _scroll;
        private List<SceneButton> _buttons = new();
        private List<string> _buildSceneNames = new();

        #endregion

    }
}
