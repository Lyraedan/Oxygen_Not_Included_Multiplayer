using System.Collections.Generic;
using UnityEngine;
using ONI_MP.DebugTools;

namespace ONI_MP.Utilities
{
    /// <summary>
    /// Manages log throttling to prevent spam while maintaining useful debugging information
    /// </summary>
    public static class ModLogThrottler
    {
        // Log throttling to reduce spam
        private static Dictionary<string, float> lastLogTime = new Dictionary<string, float>();
        private static readonly float LOG_THROTTLE_SECONDS = 5f; // Only log same message every 5 seconds

        /// <summary>
        /// Logs a message but throttles it to avoid spam
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="category">Category for grouping similar messages</param>
        public static void LogThrottled(string message, string category = "general")
        {
            float currentTime = Time.realtimeSinceStartup;
            string key = $"{category}:{message}";

            if (!lastLogTime.ContainsKey(key) || (currentTime - lastLogTime[key]) >= LOG_THROTTLE_SECONDS)
            {
                lastLogTime[key] = currentTime;
                DebugConsole.LogWarning($"[ModLogThrottler] {message}");
            }
        }

        /// <summary>
        /// Logs a regular message with throttling
        /// </summary>
        public static void LogThrottledInfo(string message, string category = "general")
        {
            float currentTime = Time.realtimeSinceStartup;
            string key = $"{category}:{message}";

            if (!lastLogTime.ContainsKey(key) || (currentTime - lastLogTime[key]) >= LOG_THROTTLE_SECONDS)
            {
                lastLogTime[key] = currentTime;
                DebugConsole.Log($"[ModLogThrottler] {message}");
            }
        }

        /// <summary>
        /// Logs a warning message with throttling
        /// </summary>
        public static void LogThrottledWarning(string message, string category = "general")
        {
            float currentTime = Time.realtimeSinceStartup;
            string key = $"{category}:{message}";

            if (!lastLogTime.ContainsKey(key) || (currentTime - lastLogTime[key]) >= LOG_THROTTLE_SECONDS)
            {
                lastLogTime[key] = currentTime;
                DebugConsole.LogWarning($"[ModLogThrottler] {message}");
            }
        }

        /// <summary>
        /// Forces a message to be logged immediately, bypassing throttling
        /// </summary>
        public static void LogForced(string message)
        {
            DebugConsole.Log($"[ModLogThrottler] {message}");
        }

        /// <summary>
        /// Forces a warning message to be logged immediately, bypassing throttling
        /// </summary>
        public static void LogForcedWarning(string message)
        {
            DebugConsole.LogWarning($"[ModLogThrottler] {message}");
        }

        /// <summary>
        /// Clears all throttling caches (useful when dialog opens/closes)
        /// </summary>
        public static void ClearThrottling()
        {
            lastLogTime.Clear();
        }

        /// <summary>
        /// Gets the current throttling status for debugging
        /// </summary>
        public static void LogThrottlingStatus()
        {
        }
    }
}