using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MenuUiControl.Editor
{
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {

        #region Init

        static PlayModeStartScene()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        #endregion


        #region Private Methods

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (EditorBuildSettings.scenes.Length == 0)
                {
                    Debug.LogWarning("[PLAY MODE]: Aucune scène dans les Build Settings.");
                    return;
                }

                // Sauvegarde le setup AVANT de changer quoi que ce soit
                _previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();

                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

                var scene0Path = EditorBuildSettings.scenes[0].path;
                EditorSceneManager.OpenScene(scene0Path, OpenSceneMode.Single);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (_previousSceneSetup != null && _previousSceneSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(_previousSceneSetup);
            }
        }

        #endregion


        #region Private

        private static SceneSetup[] _previousSceneSetup;

        #endregion

    }
}
