using UnityEngine;

namespace Core.Runtime
{
    public static class Log
    {
        public static bool Enabled = true;

        private static readonly ILogHandler _defaultHandler = Debug.unityLogger.logHandler;

        public static void Info(string msg, string tag = "")
        { if (Enabled) _defaultHandler.LogFormat(LogType.Log, null, "{0}", Format(msg, tag).Color(Color.cyan)); }

        public static void Warn(string msg, string tag = "")
        { if (Enabled) _defaultHandler.LogFormat(LogType.Warning, null, "{0}", Format(msg, tag).Color(Color.yellow)); }

        public static void Error(string msg, string tag = "")
        { if (Enabled) _defaultHandler.LogFormat(LogType.Error, null, "{0}", Format(msg, tag).Color(Color.red)); }

        public static void Success(string msg, string tag = "")
        { if (Enabled) _defaultHandler.LogFormat(LogType.Log, null, "{0}", Format(msg, tag).Color(Color.green)); }

        private static string Format(string msg, string tag)
            => string.IsNullOrEmpty(tag) ? msg : $"[{tag}] {msg}";
    }

    public static class StringExtensions
    {
        public static string Bold(this string str) => $"<b>{str}</b>";
        public static string Italic(this string str) => $"<i>{str}</i>";
        public static string Underline(this string str) => $"<u>{str}</u>";
        public static string Strikethrough(this string str) => $"<s>{str}</s>";
        public static string Size(this string str, float size) => $"<size={size}>{str}</size>";
        public static string Color(this string str, Color color)
            => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";
    }
}