using UnityEngine;
using System.Collections.Generic;

namespace ONI_MP.DebugTools
{
    public class DebugConsole : MonoBehaviour
    {
        private static DebugConsole _instance;
        private static readonly List<LogEntry> logEntries = new List<LogEntry>();
        private Vector2 scrollPos;
        private bool showConsole = false;
        private const int MaxLines = 300;

        private GUIStyle logStyle;
        private GUIStyle warnStyle;
        private GUIStyle errorStyle;

        private class LogEntry
        {
            public string message;
            public string stack;
            public LogType type;
            public bool expanded;
        }

        public static void Init()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("ONI_MP_DebugConsole");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DebugConsole>();
        }

        public void Toggle()
        {
            showConsole = !showConsole;
        }

        private void Awake()
        {
            //Application.logMessageReceived += HandleLog;
        }

        private void OnDestroy()
        {
            //Application.logMessageReceived -= HandleLog;
        }

        private void OnGUI()
        {
            if (!showConsole) return;

            if (logStyle == null)
            {
                logStyle = new GUIStyle(GUI.skin.label);
                warnStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } };
                errorStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
            }

            GUILayout.BeginArea(new Rect(Screen.width - 610, 10, 600, Screen.height - 20), GUI.skin.box);
            GUILayout.Label("<b>Console Output</b>", new GUIStyle(GUI.skin.label) { richText = true });

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var entry in logEntries)
            {
                GUIStyle style;
                switch (entry.type)
                {
                    case LogType.Warning:
                        style = warnStyle;
                        break;
                    case LogType.Error:
                    case LogType.Exception:
                    case LogType.Assert:
                        style = errorStyle;
                        break;
                    default:
                        style = logStyle;
                        break;
                }


                if (GUILayout.Button(entry.message, style))
                {
                    entry.expanded = !entry.expanded;
                }

                if (entry.expanded && !string.IsNullOrEmpty(entry.stack))
                {
                    GUILayout.Label(entry.stack, GUI.skin.box);
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Clear")) logEntries.Clear();

            GUILayout.EndArea();
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            string message = $"[{type}] {logString}";
            if (type == LogType.Log)
            {
                message = $"{logString}";
            }
            logEntries.Add(new LogEntry
            {
                message = message,
                stack = stackTrace,
                type = type,
                expanded = false
            });

            if (logEntries.Count > MaxLines)
                logEntries.RemoveAt(0);
        }

        public static void Log(string message)
        {
            Debug.Log($"[ONI_MP] {message}");
            EnsureInstance();
            _instance.HandleLog($"{message}", "", LogType.Log);
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[ONI_MP] {message}");
            EnsureInstance();
            _instance.HandleLog($"{message}", "", LogType.Warning);
        }

        public static void LogError(string message, bool trigger_error_screen = true)
        {
            if (trigger_error_screen)
            {
                Debug.LogError($"[ONI_MP] {message}");
            }
            EnsureInstance();
            _instance.HandleLog($"[ONI_MP] {message}", "", LogType.Error);
        }

        public static void LogException(System.Exception ex)
        {
            Debug.LogException(ex);
            EnsureInstance();
            _instance.HandleLog($"{ex.Message}", ex.StackTrace, LogType.Exception);
        }

        public static void LogAssert(string message)
        {
            Debug.Log($"[ONI_MP/Assert] {message}");
            EnsureInstance();
            _instance.HandleLog($"{message}", "", LogType.Assert);
        }

        private static void EnsureInstance()
        {
            if (_instance == null)
                Init();
        }
    }
}
