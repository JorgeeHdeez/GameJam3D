#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace Core.Editor
{
    public static class SortChildren
    {
        #region Menu
        [MenuItem("Managers/Sort All Children's objects In Scene", false, 0)]
        private static void SortAllSceneChildren()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            if (rootObjects.Length == 0) return;

            Undo.RegisterCompleteObjectUndo(
                rootObjects.Select(go => go.transform).ToArray(),
                "Sort All Scene Children");

            foreach (GameObject root in rootObjects)
                SortTransformChildrenRecursive(root.transform);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
        }

        [MenuItem("GameObject/Sort Children", false, 0)]
        private static void SortSelectedChildren()
        {
            if (Selection.activeGameObject == null) return;

            Undo.RegisterCompleteObjectUndo(
                Selection.activeGameObject.GetComponentsInChildren<Transform>(),
                "Sort Children");

            SortTransformChildrenRecursive(Selection.activeGameObject.transform);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());
        }

        [MenuItem("GameObject/Sort Children", true)]
        private static bool SortSelectedChildrenValidate()
        {
            return Selection.activeGameObject != null;
        }
        #endregion


        #region Private
        private static void SortTransformChildrenRecursive(Transform parent)
        {
            var children = parent.Cast<Transform>()
                .OrderBy(t => t.name)
                .ToList();

            for (int i = 0; i < children.Count; i++)
            {
                children[i].SetSiblingIndex(i);
                SortTransformChildrenRecursive(children[i]);
            }
        }
        #endregion
    }
}
#endif