#if UNITY_EDITOR
using UnityEditor;

namespace Core.Editor
{
    public static class LogManagerMenu
    {
        private const string MENU_PATH = "Managers/Log System/Enable Custom Logs";
        private const string PREFS_KEY = "LogSystem_Enabled";
        private const string UNITY_LOGS_MENU_PATH = "Managers/Log System/Enable Unity Logs";
        private const string UNITY_LOGS_PREFS_KEY = "UnityLogs_Enabled";

        [MenuItem(MENU_PATH, false, 1)]
        private static void ToggleLogs()
        {
            bool newState = !EditorPrefs.GetBool(PREFS_KEY, true);
            EditorPrefs.SetBool(PREFS_KEY, newState);
            Core.Runtime.Log.Enabled = newState;
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ToggleLogsValidate()
        {
            Menu.SetChecked(MENU_PATH, EditorPrefs.GetBool(PREFS_KEY, true));
            return true;
        }

        [MenuItem(UNITY_LOGS_MENU_PATH, false, 2)]
        private static void ToggleUnityLogs()
        {
            bool newState = !EditorPrefs.GetBool(UNITY_LOGS_PREFS_KEY, true);
            EditorPrefs.SetBool(UNITY_LOGS_PREFS_KEY, newState);
            UnityEngine.Debug.unityLogger.filterLogType = newState
                ? UnityEngine.LogType.Log
                : UnityEngine.LogType.Exception;
        }

        [MenuItem(UNITY_LOGS_MENU_PATH, true)]
        private static bool ToggleUnityLogsValidate()
        {
            Menu.SetChecked(UNITY_LOGS_MENU_PATH, EditorPrefs.GetBool(UNITY_LOGS_PREFS_KEY, true));
            return true;
        }
    }
}
#endif