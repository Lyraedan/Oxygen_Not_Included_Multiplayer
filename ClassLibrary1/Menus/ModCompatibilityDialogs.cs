using UnityEngine;
using System;
using ONI_MP.DebugTools;
using ONI_MP.Managers;
using ONI_MP.Services;
using ONI_MP.Utilities;

namespace ONI_MP.Menus
{
    /// <summary>
    /// Handles all dialog drawing and visual components for mod compatibility
    /// </summary>
    public static class ModCompatibilityDialogs
    {
        /// <summary>
        /// Draws the installation progress section
        /// </summary>
        public static void DrawInstallationProgress()
        {
            // Progress header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.wordWrap = true;
            headerStyle.normal.textColor = Color.cyan;

            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALLING, headerStyle);
            GUILayout.Space(10);

            // Status message
            string statusMessage = ModProgressTracker.InstallStatusMessage;
            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
                statusStyle.fontSize = 14;
                statusStyle.alignment = TextAnchor.MiddleCenter;
                statusStyle.wordWrap = true;
                statusStyle.normal.textColor = Color.white;

                GUILayout.Label(statusMessage, statusStyle);
                GUILayout.Space(5);
            }

            // Progress bar
            Rect progressRect = GUILayoutUtility.GetRect(250, 25);

            // Background
            GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            GUI.Box(progressRect, "");

            // Progress fill
            GUI.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            Rect fillRect = new Rect(
                progressRect.x + 2,
                progressRect.y + 2,
                (progressRect.width - 4) * ModProgressTracker.InstallProgress,
                progressRect.height - 4
            );
            GUI.Box(fillRect, "");

            // Progress text
            GUI.color = Color.white;
            GUIStyle progressTextStyle = new GUIStyle(GUI.skin.label);
            progressTextStyle.alignment = TextAnchor.MiddleCenter;
            progressTextStyle.fontStyle = FontStyle.Bold;
            progressTextStyle.fontSize = 12;

            GUI.Label(progressRect, ModProgressTracker.GetProgressText(), progressTextStyle);

            // Instructions
            GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
            instructionStyle.fontSize = 12;
            instructionStyle.alignment = TextAnchor.MiddleCenter;
            instructionStyle.wordWrap = true;
            instructionStyle.normal.textColor = Color.yellow;

            GUILayout.Space(10);
            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.PLEASE_WAIT, instructionStyle);

            GUI.color = Color.white;
        }

        /// <summary>
        /// Draws restart notification at top of screen
        /// </summary>
        public static void DrawRestartNotification()
        {
            // Check if notification should still be visible
            if (!ModRestartManager.ShouldShowRestartNotification)
                return;

            float alpha = ModRestartManager.GetNotificationAlpha();
            if (alpha <= 0f) return;

            // Position at top center of screen
            float width = 500f;
            float height = 80f;
            float x = (Screen.width - width) / 2;
            float y = 50f;

            Rect notificationRect = new Rect(x, y, width, height);

            // Draw background
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.95f * alpha);
            GUI.DrawTexture(notificationRect, Texture2D.whiteTexture);

            // Draw border
            GUI.color = new Color(1f, 0.8f, 0f, alpha); // Yellow/orange border
            GUI.Box(notificationRect, "");

            GUI.color = new Color(1f, 1f, 1f, alpha);

            // Draw text
            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 14;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.wordWrap = true;
            textStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);

            GUI.Label(notificationRect, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.MODS_ENABLED_RESTART_NOTIFICATION, textStyle);

            GUI.color = Color.white;
        }


        /// <summary>
        /// Draws a section for displaying mods with action buttons
        /// </summary>
        public static void DrawModSection(string title, string[] mods, Color color, string buttonText)
        {
            // Section title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = color;

            GUILayout.Label(title, titleStyle);

            // Mod entries
            foreach (var mod in mods)
            {
                GUILayout.BeginHorizontal();

                // Check if mod is local
                bool isLocal = ModStateManager.IsLocalMod(mod);

                // Mod name with local indicator
                GUIStyle modStyle = new GUIStyle(GUI.skin.label);
                modStyle.normal.textColor = color;
                modStyle.wordWrap = true;

                string modDisplayText = isLocal ? $"• {mod} (Local)" : $"• {mod}";
                GUILayout.Label(modDisplayText, modStyle);

                GUILayout.FlexibleSpace();

                // Handle local mods differently - they can't be subscribed to
                if (isLocal)
                {
                    // Local mods can only be enabled/disabled, never subscribed
                    if (ModStateManager.IsModInstalled(mod))
                    {
                        bool isEnabled = ModStateManager.IsModEnabled(mod);
                        string localButtonText = isEnabled ? "Disable" : "Enable";
                        Color localButtonColor = isEnabled ? Color.red : Color.green;

                        GUIStyle localModButtonStyle = new GUIStyle(GUI.skin.button);
                        localModButtonStyle.fontSize = 10;
                        localModButtonStyle.normal.textColor = localButtonColor;

                        if (GUILayout.Button(localButtonText, localModButtonStyle, GUILayout.Width(90), GUILayout.Height(20)))
                        {
                            if (isEnabled)
                            {
                                DebugConsole.Log($"[ModCompatibilityDialogs] Disabling local mod: {mod}");
                                ModInstallationService.Instance.DisableMod(mod);
                            }
                            else
                            {
                                DebugConsole.Log($"[ModCompatibilityDialogs] Enabling local mod: {mod}");
                                ModInstallationService.Instance.EnableMod(mod);
                            }
                            ModStateManager.UpdateModStateAfterOperation(mod);
                        }
                    }
                    else
                    {
                        // Local mod not found - show status
                        GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
                        statusStyle.fontSize = 10;
                        statusStyle.normal.textColor = Color.gray;
                        GUILayout.Label("Not Found", statusStyle, GUILayout.Width(90), GUILayout.Height(20));
                    }
                }
                else
                {
                    // Steam Workshop mod - normal button logic
                    var currentState = ModStateManager.GetModButtonState(mod);
                    string dynamicButtonText = ModStateManager.GetModButtonText(mod);

                    // Button color and style based on state
                    GUIStyle modButtonStyle = new GUIStyle(GUI.skin.button);
                    modButtonStyle.fontSize = 10;

                    // Set button color based on state
                    switch (currentState)
                    {
                        case ModStateManager.ModButtonState.Subscribe:
                            modButtonStyle.normal.textColor = Color.cyan;
                            break;
                        case ModStateManager.ModButtonState.Subscribing:
                            modButtonStyle.normal.textColor = Color.yellow;
                            break;
                        case ModStateManager.ModButtonState.Enable:
                            modButtonStyle.normal.textColor = Color.green;
                            break;
                        case ModStateManager.ModButtonState.Disable:
                            modButtonStyle.normal.textColor = Color.red;
                            break;
                    }

                    // Disable button during subscription
                    bool buttonEnabled = currentState != ModStateManager.ModButtonState.Subscribing;
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = buttonEnabled;

                    if (GUILayout.Button(dynamicButtonText, modButtonStyle, GUILayout.Width(90), GUILayout.Height(20)))
                    {
                        HandleDynamicModAction(mod, currentState);
                    }

                    GUI.enabled = wasEnabled;

                    // Add "Open Steam Page" button for uninstalled Steam mods
                    if (currentState == ModStateManager.ModButtonState.Subscribe)
                    {
                        GUIStyle steamButtonStyle = new GUIStyle(GUI.skin.button);
                        steamButtonStyle.fontSize = 9;
                        steamButtonStyle.normal.textColor = Color.white;

                        if (GUILayout.Button("Steam", steamButtonStyle, GUILayout.Width(50), GUILayout.Height(20)))
                        {
                            DebugConsole.Log($"[ModCompatibilityDialogs] User manually requested Steam page for mod: {mod}");
                            ModInstallationService.Instance.OpenSteamWorkshopPage(mod);
                        }
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Draws an informational section for mods (read-only)
        /// </summary>
        public static void DrawInfoSection(string title, string[] mods)
        {
            // Info title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.green;

            GUILayout.Label(title, titleStyle);

            // Mod entries
            foreach (var mod in mods)
            {
                GUIStyle modStyle = new GUIStyle(GUI.skin.label);
                modStyle.normal.textColor = Color.green;
                modStyle.wordWrap = true;

                GUILayout.Label($"• {mod}", modStyle);
            }
        }

        /// <summary>
        /// Handles mod actions based on dynamic button state
        /// </summary>
        private static void HandleDynamicModAction(string modDisplayName, ModStateManager.ModButtonState currentState)
        {
            try
            {
                DebugConsole.Log($"[ModCompatibilityDialogs] User clicked {currentState} for mod: {modDisplayName}");

                switch (currentState)
                {
                    case ModStateManager.ModButtonState.Subscribe:
                        // Subscribe to mod and set to subscribing state
                        DebugConsole.Log($"[ModCompatibilityDialogs] Starting subscription to mod: {modDisplayName}");
                        ModStateManager.SetModSubscribing(modDisplayName);
                        ModInstallationService.Instance.SubscribeSingleMod(
                            modDisplayName,
                            () => {
                                ModStateManager.UpdateModStateAfterOperation(modDisplayName);
                            },
                            error => {
                                DebugConsole.LogWarning($"[ModCompatibilityDialogs] Subscribe failed: {error}");
                                ModStateManager.UpdateModStateAfterOperation(modDisplayName);
                            }
                        );
                        break;

                    case ModStateManager.ModButtonState.Subscribing:
                        // Button should be disabled during subscription, this shouldn't happen
                        DebugConsole.LogWarning($"[ModCompatibilityDialogs] Subscribe button clicked while subscribing: {modDisplayName}");
                        break;

                    case ModStateManager.ModButtonState.Enable:
                        // Enable installed but disabled mod
                        DebugConsole.Log($"[ModCompatibilityDialogs] Enabling mod: {modDisplayName}");
                        ModInstallationService.Instance.EnableMod(modDisplayName);
                        ModStateManager.UpdateModStateAfterOperation(modDisplayName);

                        // Mark mods as modified but don't show restart prompt immediately
                        // User will see restart prompt when clicking "Apply"
                        if (ModRestartManager.ModsWereModified)
                        {
                            DebugConsole.Log($"[ModCompatibilityDialogs] Mod {modDisplayName} enabled - changes marked for apply");
                        }
                        break;

                    case ModStateManager.ModButtonState.Disable:
                        // Disable enabled mod
                        DebugConsole.Log($"[ModCompatibilityDialogs] Disabling mod: {modDisplayName}");
                        ModInstallationService.Instance.DisableMod(modDisplayName);
                        ModStateManager.UpdateModStateAfterOperation(modDisplayName);

                        // Mark mods as modified but don't show restart prompt immediately
                        // User will see restart prompt when clicking "Apply"
                        if (ModRestartManager.ModsWereModified)
                        {
                            DebugConsole.Log($"[ModCompatibilityDialogs] Mod {modDisplayName} disabled - changes marked for apply");
                        }
                        break;

                    default:
                        DebugConsole.LogWarning($"[ModCompatibilityDialogs] Unknown button state {currentState} for mod: {modDisplayName}");
                        ModInstallationService.Instance.OpenSteamWorkshopPage(modDisplayName);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityDialogs] Error in HandleDynamicModAction for {modDisplayName}: {ex.Message}");

                // Reset mod state on error
                ModStateManager.UpdateModStateAfterOperation(modDisplayName);
            }
        }

        /// <summary>
        /// Draws action buttons for subscribe all and enable all
        /// </summary>
        public static void DrawActionButtons(string[] missingMods)
        {
            if (missingMods == null || missingMods.Length == 0)
                return;

            // Check if there are truly missing mods or just disabled ones
            bool hasTrulyMissing = false;
            bool hasDisabled = false;
            int steamModsCount = 0;

            foreach (var mod in missingMods)
            {
                bool isLocal = ModStateManager.IsLocalMod(mod);

                if (ModStateManager.IsModEnabled(mod))
                {
                    // Mod is enabled, ignore
                    continue;
                }
                else if (ModStateManager.IsModInstalled(mod))
                {
                    hasDisabled = true;
                }
                else
                {
                    hasTrulyMissing = true;
                    // Only count Steam mods for subscription
                    if (!isLocal)
                    {
                        steamModsCount++;
                    }
                }
            }

            // Subscribe All button (only for Steam Workshop mods)
            if (steamModsCount > 0)
            {
                GUIStyle subscribeAllStyle = new GUIStyle(GUI.skin.button);
                subscribeAllStyle.fontSize = 12;
                subscribeAllStyle.fontStyle = FontStyle.Bold;
                subscribeAllStyle.normal.textColor = Color.cyan;

                // Show button text with count of Steam mods
                string subscribeAllText = $"Subscribe All ({steamModsCount} Steam mods)";
                if (GUILayout.Button(subscribeAllText, subscribeAllStyle, GUILayout.Height(35)))
                {
                    DebugConsole.Log($"[ModCompatibilityDialogs] User clicked Subscribe All - will subscribe to {steamModsCount} Steam mods");

                    // Filter to only include Steam mods for subscription
                    var steamModsToSubscribe = new System.Collections.Generic.List<string>();
                    foreach (var mod in missingMods)
                    {
                        if (!ModStateManager.IsModEnabled(mod) && !ModStateManager.IsModInstalled(mod) && !ModStateManager.IsLocalMod(mod))
                        {
                            steamModsToSubscribe.Add(mod);
                        }
                    }

                    // Start progress tracking
                    ModProgressTracker.StartInstallationProgress(steamModsCount, "Starting subscription to Steam mods...");

                    // Subscribe to Steam mods only
                    ModInstallationService.Instance.SubscribeAllMods(
                        steamModsToSubscribe.ToArray(),
                        (completed, total) => {
                            ModProgressTracker.UpdateInstallationProgress(completed, total, $"Subscribing to Steam mods... {completed}/{total}");
                        },
                        (successful, failed) => {
                            string message = $"Steam subscription complete: {successful.Count} successful, {failed.Count} failed";
                            ModProgressTracker.CompleteInstallationProgress(message);
                            ModProgressTracker.HideProgressAfterDelay(3f, ModInstallationService.Instance);
                        }
                    );
                }

                GUILayout.Space(10);
            }

            // Enable All button (only for installed but disabled mods)
            if (hasDisabled)
            {
                // Count disabled mods for clear user feedback
                int disabledCount = 0;
                foreach (var mod in missingMods)
                {
                    if (ModStateManager.IsModInstalled(mod) && !ModStateManager.IsModEnabled(mod))
                    {
                        disabledCount++;
                    }
                }

                GUIStyle enableAllStyle = new GUIStyle(GUI.skin.button);
                enableAllStyle.fontSize = 12;
                enableAllStyle.fontStyle = FontStyle.Bold;
                enableAllStyle.normal.textColor = Color.green;

                // Show transparent button text with count
                string enableAllText = $"{MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ENABLE_ALL} ({disabledCount} mods)";
                if (GUILayout.Button(enableAllText, enableAllStyle, GUILayout.Height(35)))
                {
                    DebugConsole.Log($"[ModCompatibilityDialogs] User clicked Enable All - will enable {disabledCount} mods");
                    ModInstallationService.Instance.EnableAllMods(missingMods);
                }

                GUILayout.Space(10);
            }
        }

        /// <summary>
        /// Creates a colored texture for UI elements
        /// </summary>
        private static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}