using System;
using UnityEngine;
using ONI_MP.DebugTools;

namespace ONI_MP.Managers
{
    /// <summary>
    /// Manages game restart functionality and notifications when mods are modified
    /// </summary>
    public static class ModRestartManager
    {
        // Restart notification fields
        private static bool showRestartNotification = false;
        private static float restartNotificationTime = 0f;
        private const float NOTIFICATION_DURATION = 5f; // Show for 5 seconds

        // Track if any mods were modified during this session
        private static bool modsWereModified = false;


        /// <summary>
        /// Gets whether restart notification should be shown
        /// </summary>
        public static bool ShouldShowRestartNotification => showRestartNotification;


        /// <summary>
        /// Gets whether mods were modified in this session
        /// </summary>
        public static bool ModsWereModified => modsWereModified;

        /// <summary>
        /// Gets restart notification time for fade calculations
        /// </summary>
        public static float RestartNotificationTime => restartNotificationTime;

        /// <summary>
        /// Gets notification duration
        /// </summary>
        public static float NotificationDuration => NOTIFICATION_DURATION;

        /// <summary>
        /// Marks that mods have been modified and restart is needed
        /// </summary>
        public static void MarkModsModified()
        {
            modsWereModified = true;
            DebugConsole.Log("[ModRestartManager] Mods marked as modified - restart will be required");
        }

        /// <summary>
        /// Shows restart notification
        /// </summary>
        public static void ShowRestartNotification()
        {
            try
            {
                DebugConsole.Log("[ModRestartManager] Showing restart notification");
                showRestartNotification = true;
                restartNotificationTime = Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModRestartManager] Failed to show restart notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates restart notification state (call from Update loop)
        /// </summary>
        public static void UpdateRestartNotification()
        {
            if (showRestartNotification)
            {
                // Check if notification should still be visible
                float elapsed = Time.realtimeSinceStartup - restartNotificationTime;
                if (elapsed > NOTIFICATION_DURATION)
                {
                    showRestartNotification = false;
                }
            }
        }


        /// <summary>
        /// Triggers game restart using ONI's native restart system
        /// </summary>
        public static void TriggerGameRestart()
        {
            try
            {
                DebugConsole.Log("[ModRestartManager] Triggering game restart...");

                // Save mod configuration first
                var modManager = Global.Instance?.modManager;
                if (modManager != null)
                {
                    modManager.Save();
                    DebugConsole.Log("[ModRestartManager] Mod configuration saved");
                }

                // Trigger restart via App.instance (ONI standard way)
                if (App.instance != null)
                {
                    DebugConsole.Log("[ModRestartManager] Restarting game via App.instance.Restart()");
                    App.instance.Restart();
                }
                else
                {
                    DebugConsole.LogWarning("[ModRestartManager] App.instance not available - manual restart required");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModRestartManager] Error triggering restart: {ex.Message}");
                DebugConsole.Log("[ModRestartManager] Please restart the game manually to apply mod changes");
            }
        }

        /// <summary>
        /// Saves mod configuration and triggers automatic game restart
        /// </summary>
        public static void SaveAndRestart()
        {
            try
            {
                DebugConsole.Log("[ModRestartManager] Saving mod configuration and triggering restart...");

                var modManager = Global.Instance?.modManager;
                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModRestartManager] ModManager not available for save and restart");
                    return;
                }

                // Save mod configuration - this should trigger automatic restart
                modManager.Save();

                // Additional restart trigger if needed
                try
                {
                    // Force restart via App.instance if modManager.Save() doesn't work
                    if (App.instance != null)
                    {
                        DebugConsole.Log("[ModRestartManager] Triggering manual restart via App.instance...");
                        App.instance.Restart();
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[ModRestartManager] Manual restart failed: {ex.Message}");
                    DebugConsole.Log("[ModRestartManager] Please restart the game manually to apply mod changes.");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModRestartManager] Error in SaveAndRestart: {ex.Message}");
                DebugConsole.Log("[ModRestartManager] Please restart the game manually to apply mod changes.");
            }
        }

        /// <summary>
        /// Checks if restart is needed and handles it appropriately
        /// </summary>
        public static void HandleRestartIfNeeded()
        {
            if (modsWereModified)
            {
                try
                {
                    var modManager = Global.Instance?.modManager;
                    if (modManager != null)
                    {
                        DebugConsole.Log("[ModRestartManager] Mods were modified. Saving and restarting game...");
                        SaveAndRestart();
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[ModRestartManager] Error handling restart: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Resets all restart tracking
        /// </summary>
        public static void Reset()
        {
            showRestartNotification = false;
            modsWereModified = false;
            restartNotificationTime = 0f;

            DebugConsole.Log("[ModRestartManager] Restart manager reset");
        }

        /// <summary>
        /// Gets the fade alpha for restart notification
        /// </summary>
        /// <returns>Alpha value (0.0 to 1.0) for fade effect</returns>
        public static float GetNotificationAlpha()
        {
            if (!showRestartNotification) return 0f;

            float elapsed = Time.realtimeSinceStartup - restartNotificationTime;

            // Calculate fade out for last second
            if (elapsed > NOTIFICATION_DURATION - 1f)
            {
                return NOTIFICATION_DURATION - elapsed; // Fade from 1 to 0 in last second
            }

            return 1f;
        }

        /// <summary>
        /// Forces restart notification to hide immediately
        /// </summary>
        public static void HideRestartNotification()
        {
            showRestartNotification = false;
            DebugConsole.Log("[ModRestartManager] Restart notification hidden manually");
        }

        /// <summary>
        /// Gets detailed status for debugging
        /// </summary>
        public static void LogStatus()
        {
            DebugConsole.Log($"[ModRestartManager] Status: ModsModified={modsWereModified}, " +
                           $"ShowNotification={showRestartNotification}");
        }
    }
}