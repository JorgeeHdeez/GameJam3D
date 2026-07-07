using System.Collections.Generic;
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

                _previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();

                Debug.Log($"[PLAY MODE]: Setup sauvegardé — {_previousSceneSetup.Length} scène(s) :");
                foreach (var s in _previousSceneSetup)
                    Debug.Log($"  → {s.path} | isLoaded={s.isLoaded} | isActive={s.isActive}");

                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log($"[PLAY MODE]: EnteredEditMode — setup à restaurer : {(_previousSceneSetup == null ? "NULL" : _previousSceneSetup.Length.ToString())}");

                if (_previousSceneSetup == null || _previousSceneSetup.Length == 0) return;

                var valid = new List<SceneSetup>();
                foreach (var setup in _previousSceneSetup)
                {
                    var guid = AssetDatabase.AssetPathToGUID(setup.path);
                    Debug.Log($"  → {setup.path} | guid={guid}");
                    if (!string.IsNullOrEmpty(guid))
                        valid.Add(setup);
                    else
                        Debug.LogWarning($"[PLAY MODE]: Scène introuvable ignorée : {setup.path}");
                }

                Debug.Log($"[PLAY MODE]: {valid.Count} scène(s) valide(s) à restaurer");

                if (valid.Count > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(valid.ToArray());
            }
        }

        #endregion


        #region Private

        private static SceneSetup[] _previousSceneSetup;

        #endregion

    }
}
