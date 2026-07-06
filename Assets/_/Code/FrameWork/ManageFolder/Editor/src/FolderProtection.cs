using UnityEditor;
using UnityEngine;

namespace ManageFolders.Editor
{
    public class FolderProtection : UnityEditor.AssetModificationProcessor
    {
        private static readonly string[] _protectedPaths =
        {
            "Assets/_",
            "Assets/_/Code",
            "Assets/_/Code/Framework",
            "Assets/_/Code/Game",
            "Assets/_/Content",
            "Assets/_/Database",
            "Assets/_/Database/Prefabs",
            "Assets/_/Database/Settings",
            "Assets/_/Scenes"
        };

        public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            foreach (var protected_path in _protectedPaths)
            {
                if (path == protected_path)
                {
                    Debug.LogWarning($"[FOLDER PROTECTION]: '{path}' est protégé et ne peut pas être supprimé.");
                    return AssetDeleteResult.FailedDelete;
                }
            }
            return AssetDeleteResult.DidNotDelete;
        }

        public static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            foreach (var protected_path in _protectedPaths)
            {
                if (sourcePath == protected_path)
                {
                    Debug.LogWarning($"[FOLDER PROTECTION]: '{sourcePath}' est protégé et ne peut pas être renommé ou déplacé.");
                    return AssetMoveResult.FailedMove;
                }
            }
            return AssetMoveResult.DidNotMove;
        }
    }
}