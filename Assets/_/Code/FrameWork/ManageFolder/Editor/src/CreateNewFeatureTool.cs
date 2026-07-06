using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ManageFolders.Editor
{
    public class CreateNewFeatureTool : EditorWindow
    {
        #region GUI
        public static void DrawGUI(ref string featureName, ref AssemblyDefinitionType assemblyType, ref bool isRuntime, ref bool isEditor, ref bool addCoreRuntime, ref bool addCoreEditor)
        {
            featureName = EditorGUILayout.TextField("Feature Name", featureName);
            EditorGUILayout.Space(5);
            assemblyType = (AssemblyDefinitionType)EditorGUILayout.EnumPopup("Assembly Type", assemblyType);
            isRuntime = EditorGUILayout.Toggle("Runtime Scope", isRuntime);
            isEditor = EditorGUILayout.Toggle("Editor Scope", isEditor);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Core References", EditorStyles.boldLabel);
            addCoreRuntime = EditorGUILayout.Toggle("Core.Runtime", addCoreRuntime);
            EditorGUI.BeginDisabledGroup(!isEditor);
            addCoreEditor = EditorGUILayout.Toggle("Core.Editor", addCoreEditor);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(featureName));
            if (GUILayout.Button("Generate Feature", GUILayout.Height(30)))
            {
                Debug.Log($"[FEATURE CREATOR]: featureName='{featureName}' runtime={isRuntime} editor={isEditor} coreRuntime={addCoreRuntime} coreEditor={addCoreEditor}");
                var tool = CreateInstance<CreateNewFeatureTool>();
                tool._featureName = featureName;
                tool._assemblyType = assemblyType;
                tool._isRuntimeScope = isRuntime;
                tool._isEditorScope = isEditor;
                tool._addCoreRuntime = addCoreRuntime;
                tool._addCoreEditor = addCoreEditor;
                tool.CreateStructure();
            }
            EditorGUI.EndDisabledGroup();
        }
        #endregion

        #region Private
        internal string _featureName;
        internal AssemblyDefinitionType _assemblyType;
        internal bool _isEditorScope;
        internal bool _isRuntimeScope = true;
        internal bool _addCoreRuntime = true;
        internal bool _addCoreEditor = true;

        private void CreateStructure()
        {
            if (string.IsNullOrEmpty(_featureName))
            {
                EditorUtility.DisplayDialog("Feature Creator", "Feature Name ne peut pas être vide", "OK");
                return;
            }
            if (!IsAssemblyNameValid(_featureName))
            {
                EditorUtility.DisplayDialog("Feature Creator", $"'{_featureName}' contient des caractères invalides.", "OK");
                return;
            }
            if (!_isRuntimeScope && !_isEditorScope)
            {
                EditorUtility.DisplayDialog("Feature Creator", "Au moins un scope doit être sélectionné", "OK");
                return;
            }

            if (_isRuntimeScope) CreateScope(AssemblyDefinitionScope.Runtime);
            if (_isEditorScope) CreateScope(AssemblyDefinitionScope.Editor);

            AssetDatabase.Refresh();
            Debug.Log($"[FEATURE CREATOR]: '{_featureName}' créé avec succès");
        }

        private void CreateScope(AssemblyDefinitionScope scope)
        {
            string rootPath = $"Assets/_/Code/{_assemblyType}";
            string basePath = $"{rootPath}/{_featureName}";
            string scopePath = $"{basePath}/{scope}";
            string srcPath = $"{scopePath}/src";

            if (!AssetDatabase.IsValidFolder(basePath))
                AssetDatabase.CreateFolder(rootPath, _featureName);
            if (!AssetDatabase.IsValidFolder(scopePath))
                AssetDatabase.CreateFolder(basePath, scope.ToString());
            if (!AssetDatabase.IsValidFolder(srcPath))
                AssetDatabase.CreateFolder(scopePath, "src");

            CreateAssemblyDefinition(scopePath, scope);

            EditorUtility.FocusProjectWindow();
            var obj = AssetDatabase.LoadAssetAtPath(basePath, typeof(UnityEngine.Object));
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        private void CreateAssemblyDefinition(string path, AssemblyDefinitionScope scope)
        {
            string scopeName = scope.ToString();
            string fileName = $"{_featureName}.{scopeName}.asmdef";

            var references = new List<string>();

            if (scope == AssemblyDefinitionScope.Editor)
            {
                if (_addCoreEditor) references.Add("Core.Editor");
            }

            if (_addCoreRuntime) references.Add("Core.Runtime");

            var data = new AssemblyDefinitionData
            {
                name = $"{_featureName}.{scopeName}",
                rootNamespace = $"{_featureName}.{scopeName}",
                references = references.ToArray(),
                autoReferenced = scope != AssemblyDefinitionScope.Runtime
            };

            if (scope == AssemblyDefinitionScope.Editor)
                data.includePlatforms = new[] { "Editor" };

            try
            {
                File.WriteAllText($"{path}/{fileName}", JsonUtility.ToJson(data));
            }
            catch (Exception e)
            {
                Debug.LogError($"[FEATURE CREATOR]: Erreur création .asmdef — {e.Message}");
            }
        }

        private bool IsAssemblyNameValid(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9_\-]+$");
        }
        #endregion
    }

    public class AssemblyDefinitionData
    {
        public string name;
        public string rootNamespace;
        public string[] references;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences;
        public bool autoReferenced = true;
        public string[] defineConstraints;
        public string[] versionDefines;
        public bool noEngineReferences;
    }

    public enum AssemblyDefinitionType { Game, Framework }
    public enum AssemblyDefinitionScope { Editor, Runtime }
}