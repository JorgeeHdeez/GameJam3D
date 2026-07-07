using Enemy.Runtime;
using UnityEditor;
using UnityEngine;

namespace Enemy.Editor
{
    /// <summary>
    /// Scene-view visualisation for <see cref="EnemyPerception"/>: a filled vision
    /// cone with a wire outline, coloured by the current awareness state, plus a
    /// line of sight to the target. Drawn with Handles for a cleaner result than
    /// runtime gizmos. Editor-only, isolated in the Enemy.Editor assembly.
    /// </summary>
    [CustomEditor(typeof(EnemyPerception))]
    public sealed class EnemyPerceptionEditor : UnityEditor.Editor
    {
        #region Unity Callbacks

        private void OnSceneGUI()
        {
            var perception = (EnemyPerception)target;

            float distance = serializedObject.FindProperty("_viewDistance").floatValue;
            float halfAngle = serializedObject.FindProperty("_viewHalfAngle").floatValue;
            float eyeHeight = serializedObject.FindProperty("_eyeHeight").floatValue;
            float targetHeight = serializedObject.FindProperty("_targetHeight").floatValue;
            var targetTransform = serializedObject.FindProperty("_target").objectReferenceValue as Transform;

            Transform origin = perception.transform;
            Vector3 eye = origin.position + Vector3.up * eyeHeight;
            Vector3 from = Quaternion.Euler(0.0f, -halfAngle, 0.0f) * origin.forward;
            float sweep = halfAngle * 2.0f;

            Color color = ResolveColor(perception.CurrentState);

            Handles.color = new Color(color.r, color.g, color.b, FillAlpha);
            Handles.DrawSolidArc(eye, Vector3.up, from, sweep, distance);

            Handles.color = color;
            Handles.DrawWireArc(eye, Vector3.up, from, sweep, distance);

            if (targetTransform == null) return;

            Handles.color = Color.white;
            Handles.DrawDottedLine(eye, targetTransform.position + Vector3.up * targetHeight, DottedLineSize);
        }

        #endregion


        #region Private Constants

        private const float FillAlpha = 0.12f;
        private const float DottedLineSize = 4.0f;

        #endregion


        #region Private Methods

        private static Color ResolveColor(PerceptionState state) => state switch
        {
            PerceptionState.Alerted => Color.red,
            PerceptionState.Suspicious => new Color(1.0f, 0.6f, 0.0f),
            _ => Color.cyan
        };

        #endregion
    }
}