using System;
using System.Collections.Generic;
using UnityEngine;
using ImGuiNET;
using System.Linq;

namespace ONI_MP.DebugTools
{
    public class DebugConsole
    {
        private static DebugConsole _instance;
        private static readonly List<LogEntry> logEntries = new List<LogEntry>();
        private static readonly object _lock = new object();

        private Vector2 scrollPos;
        private bool autoScroll = true;
        private bool collapseDuplicates = false;
        private string filter = "";

        private const int MaxLines = 300;
        private bool showConsole = false;

        private class LogEntry
        {
            public string message;
            public string stack;
            public LogType type;
            public bool expanded;
            public int count = 1;
        }

        public enum LogType
        {
            Error,
            Assert,
            Warning,
            Log,
            Exception,
            Success,
            NonImportant
        }

        public static DebugConsole Init()
        {
            _instance = new DebugConsole();
            return _instance;
        }

        public static void Log(string message)
        {
            Debug.Log($"[ONI_MP] {message}");
            EnsureInstance();
            _instance.AddLog(message, "", LogType.Log);
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[ONI_MP] {message}");
            EnsureInstance();
            _instance.AddLog(message, "", LogType.Warning);
        }

        public static void LogError(string message, bool trigger_error_screen = true)
        {
            if (trigger_error_screen)
                Debug.LogError($"[ONI_MP] {message}");

            EnsureInstance();
            _instance.AddLog(message, "", LogType.Error);
        }

        public static void LogException(Exception ex)
        {
            Debug.LogException(ex);
            EnsureInstance();
            _instance.AddLog(ex.Message, ex.StackTrace, LogType.Exception);
        }

        public static void LogAssert(string message)
        {
            Debug.Log($"[ONI_MP/Assert] {message}");
            EnsureInstance();
            _instance.AddLog(message, "", LogType.Assert);
        }

        public static void LogSuccess(string message)
        {
            Debug.Log($"[ONI_MP] {message}");
            EnsureInstance();
            _instance.AddLog(message, "", LogType.Success);
        }

        public static void LogNonImportant(string message)
        {
            Debug.Log($"[ONI_MP] {message}");
            EnsureInstance();
            _instance.AddLog(message, "", LogType.NonImportant);
        }

        private static void EnsureInstance()
        {
            _instance = new DebugConsole();
        }

        private void AddLog(string message, string stack, LogType type)
        {
            lock (_lock)
            {
                if (collapseDuplicates && logEntries.Count > 0)
                {
                    var last = logEntries[logEntries.Count - 1];
                    if (last.message == message && last.type == type)
                    {
                        last.count++;
                        return;
                    }
                }

                logEntries.Add(new LogEntry
                {
                    message = message,
                    stack = stack,
                    type = type,
                    expanded = false
                });

                if (logEntries.Count > MaxLines)
                    logEntries.RemoveAt(0);
            }
        }

        /// <summary>
        /// Toggles visibility of the ImGui console window.
        /// </summary>
        public void Toggle()
        {
            showConsole = !showConsole;
        }

        /// <summary>
        /// Draws the ImGui window for the debug console.
        /// Call this from your DevTool.RenderTo() or ImGui render loop.
        /// </summary>
        public void ShowWindow()
        {
            if (!showConsole)
                return;

            if (ImGui.Begin("Multiplayer Console", ref showConsole, ImGuiWindowFlags.MenuBar))
            {
                // Toolbar
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.Button("Clear"))
                    {
                        lock (_lock) { logEntries.Clear(); }
                    }

                    ImGui.SameLine();
                    ImGui.Checkbox("Auto-scroll", ref autoScroll);
                    ImGui.SameLine();
                    ImGui.Checkbox("Collapse", ref collapseDuplicates);
                    ImGui.SameLine();
                    ImGui.InputText("Filter", ref filter, 128);

                    ImGui.EndMenuBar();
                }

                ImGui.Separator();

                // Scroll region
                ImGui.BeginChild("ConsoleScroll", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);

                lock (_lock)
                {
                    foreach (var entry in logEntries)
                    {
                        if (!string.IsNullOrEmpty(filter) && entry.message.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        Vector4 color = new Vector4(1f, 1f, 1f, 1f);
                        switch (entry.type)
                        {
                            case LogType.Warning:
                                color = new Vector4(1f, 1f, 0.3f, 1f);
                                break;
                            case LogType.Error:
                                color = new Vector4(1f, 0.4f, 0.4f, 1f);
                                break;
                            case LogType.Assert:
                                color = new Vector4(0.8f, 0.5f, 1f, 1f);
                                break;
                            case LogType.Exception:
                                color = new Vector4(1f, 0.4f, 0.4f, 1f);
                                break;
                            case LogType.Success:
                                color = new Vector4(0f, 1f, 0f, 1f);
                                break;
                            case LogType.NonImportant:
                                color = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
                                break;
                            default:
                                break;
                        }

                        string displayMsg = entry.count > 1 ? $"{entry.message} (x{entry.count})" : entry.message;

                        ImGui.TextColored(color, displayMsg);
                    }
                }

                if (autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                    ImGui.SetScrollHereY(1.0f);

                ImGui.EndChild();
            }

            ImGui.End();
        }
    }
}
