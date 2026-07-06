#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Core.Editor
{
    public static class LogManager
    {
        private const string PREFS_KEY = "LogSystem_Enabled";
        private const string UNITY_LOGS_PREFS_KEY = "UnityLogs_Enabled";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
#if UNITY_EDITOR
            Core.Runtime.Log.Enabled = EditorPrefs.GetBool(PREFS_KEY, true);
            bool unityLogsEnabled = EditorPrefs.GetBool(UNITY_LOGS_PREFS_KEY, true);
#else
            Core.Runtime.Log.Enabled = true;
            bool unityLogsEnabled = true;
#endif
            Debug.unityLogger.filterLogType = unityLogsEnabled
                ? LogType.Log
                : LogType.Exception;
        }
    }
}