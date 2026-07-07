using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

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

                var setup = EditorSceneManager.GetSceneManagerSetup();
                var paths = new List<string>();
                var loaded = new List<bool>();
                var active = new List<string>();

                string activePath = "";
                foreach (var s in setup)
                {
                    paths.Add(s.path);
                    loaded.Add(s.isLoaded);
                    if (s.isActive) activePath = s.path;
                }

                SessionState.SetString(_sessionKey, string.Join("|", paths));
                SessionState.SetString(_sessionLoadedKey, string.Join("|", loaded));
                SessionState.SetString(_sessionActiveKey, activePath);

                Debug.Log($"[PLAY MODE]: {paths.Count} scène(s) sauvegardées en SessionState");

                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                var pathsRaw = SessionState.GetString(_sessionKey, "");
                var loadedRaw = SessionState.GetString(_sessionLoadedKey, "");
                var activePath = SessionState.GetString(_sessionActiveKey, "");

                Debug.Log($"[PLAY MODE]: EnteredEditMode — paths={pathsRaw}");

                if (string.IsNullOrEmpty(pathsRaw)) return;

                var paths = pathsRaw.Split('|');
                var loadedArr = loadedRaw.Split('|');

                var valid = new List<SceneSetup>();
                for (int i = 0; i < paths.Length; i++)
                {
                    var path = paths[i];
                    if (string.IsNullOrEmpty(path)) continue;
                    if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                    {
                        Debug.LogWarning($"[PLAY MODE]: Scène introuvable ignorée : {path}");
                        continue;
                    }

                    bool isLoaded = i < loadedArr.Length && loadedArr[i] == "True";
                    valid.Add(new SceneSetup
                    {
                        path = path,
                        isLoaded = isLoaded,
                        isActive = path == activePath
                    });
                }

                Debug.Log($"[PLAY MODE]: {valid.Count} scène(s) restaurées");

                if (valid.Count > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(valid.ToArray());

                SessionState.EraseString(_sessionKey);
                SessionState.EraseString(_sessionLoadedKey);
                SessionState.EraseString(_sessionActiveKey);
            }
        }

        #endregion


        #region Private

        private const string _sessionKey = "PlayModeStartScene_paths";
        private const string _sessionLoadedKey = "PlayModeStartScene_loaded";
        private const string _sessionActiveKey = "PlayModeStartScene_active";

        #endregion

    }
}
