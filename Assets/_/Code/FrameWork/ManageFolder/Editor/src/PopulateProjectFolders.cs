using UnityEditor;
using UnityEngine;

namespace ManageFolders.Editor
{
    public class PopulateProjectFolders : EditorWindow
    {
        #region Menu
        [MenuItem("Managers/Manage Folders")]
        public static void ShowWindow() => GetWindow<PopulateProjectFolders>("Manage Folders");
        #endregion


        #region GUI
        private void OnGUI()
        {
            // --- Folders ---
            EditorGUILayout.LabelField("Folders", EditorStyles.boldLabel);
            if (GUILayout.Button("Populate Folders", GUILayout.Height(30)))
                PopulateFolders();

            EditorGUILayout.Space(15);

            // --- Feature Creator ---
            EditorGUILayout.LabelField("Feature Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            CreateNewFeatureTool.DrawGUI(
                ref _featureName,
                ref _assemblyType,
                ref _isRuntimeScope,
                ref _isEditorScope,
                ref _addCoreRuntime,
                ref _addCoreEditor);
        }
        #endregion


        #region Private — Folders
        private const string _subRoot = "_";
        private readonly string[] _mainFolders = { "Code", "Content", "Database" };
        private readonly string[] _codeSubFolders = { "Framework", "Game" };
        private readonly string[] _contentSubFolders = { "" };
        private readonly string[] _databaseSubFolders = { "Prefabs", "Scenes", "Settings" };

        private void PopulateFolders()
        {
            string currentPath = "Assets";
            if (!AssetDatabase.IsValidFolder($"{currentPath}/{_subRoot}"))
                AssetDatabase.CreateFolder(currentPath, _subRoot);
            currentPath += $"/{_subRoot}";
            IterateThrough(_mainFolders, currentPath);
            IterateThrough(_codeSubFolders, currentPath + "/Code");
            IterateThrough(_contentSubFolders, currentPath + "/Content");
            IterateThrough(_databaseSubFolders, currentPath + "/Database");

            MigrateFolderContents("Assets/Settings", "Assets/_/Database/Settings");
            MigrateFolderContents("Assets/Scenes", "Assets/_/Database/Scenes/_OriginalScenes");
        }

        private void IterateThrough(string[] foldersList, string path)
        {
            for (int i = 0; i < foldersList.Length; i++)
            {
                if (foldersList[i] == "") return;
                if (AssetDatabase.IsValidFolder($"{path}/{foldersList[i]}")) continue;
                if (AssetDatabase.IsValidFolder(path))
                    AssetDatabase.CreateFolder(path, foldersList[i]);
            }
        }

        private void MigrateFolderContents(string sourceFolder, string destFolder)
        {
            if (!AssetDatabase.IsValidFolder(sourceFolder)) return;

            if (!AssetDatabase.IsValidFolder(destFolder))
            {
                var parts = destFolder.Split('/');
                var path = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = $"{path}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(path, parts[i]);
                    path = next;
                }
            }

            var guids = AssetDatabase.FindAssets("", new[] { sourceFolder });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath == sourceFolder) continue;

                var relativePath = assetPath.Replace(sourceFolder + "/", "");
                if (relativePath.Contains("/")) continue;

                var fileName = System.IO.Path.GetFileName(assetPath);
                var dest = $"{destFolder}/{fileName}";

                if (System.IO.File.Exists(dest))
                {
                    Debug.LogWarning($"[POPULATE]: '{fileName}' existe déjà dans '{destFolder}' — ignoré");
                    continue;
                }

                var error = AssetDatabase.MoveAsset(assetPath, dest);
                if (string.IsNullOrEmpty(error))
                    Debug.Log($"[POPULATE]: '{fileName}' déplacé vers '{destFolder}'");
                else
                    Debug.LogError($"[POPULATE]: Erreur déplacement '{fileName}' — {error}");
            }

            var remaining = AssetDatabase.FindAssets("", new[] { sourceFolder });
            if (remaining.Length == 0)
            {
                AssetDatabase.DeleteAsset(sourceFolder);
                Debug.Log($"[POPULATE]: Dossier '{sourceFolder}' supprimé");
            }

            AssetDatabase.Refresh();
        }
        #endregion


        #region Private — Feature Creator
        private string _featureName;
        private AssemblyDefinitionType _assemblyType;
        private bool _isEditorScope;
        private bool _isRuntimeScope = true;
        private bool _addCoreRuntime = true;
        private bool _addCoreEditor = true;
        #endregion
    }
}