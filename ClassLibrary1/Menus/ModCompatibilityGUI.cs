using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ONI_MP.DebugTools;
using ONI_MP.Networking;

namespace ONI_MP.Menus
{
    public class ModCompatibilityGUI : MonoBehaviour
    {
        private static ModCompatibilityGUI instance;
        private static bool showDialog = false;
        private static string dialogReason = "";
        private static string[] dialogMissingMods = null;
        private static string[] dialogExtraMods = null;
        private static string[] dialogVersionMismatches = null;
        private static ulong[] dialogSteamModIds = null;

        private Vector2 scrollPosition = Vector2.zero;
        private Rect windowRect = new Rect(0, 0, 600, 400);

        // Restart notification fields
        private static bool showRestartNotification = false;
        private static float restartNotificationTime = 0f;
        private const float NOTIFICATION_DURATION = 5f; // Show for 5 seconds

        // Track if mods were enabled and need restart
        private static bool allModsEnabled = false;

        // Track if any mods were modified during this session
        private static bool modsWereModified = false;

        public static void ShowIncompatibilityError(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches, ulong[] steamModIds)
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Showing compatibility dialog with IMGUI");
                DebugConsole.Log($"[ModCompatibilityGUI] Steam mod IDs for auto-install: {steamModIds?.Length ?? 0}");

                // Store dialog data
                dialogReason = reason ?? "";
                dialogMissingMods = missingMods ?? new string[0];
                dialogExtraMods = extraMods ?? new string[0];
                dialogVersionMismatches = versionMismatches ?? new string[0];
                dialogSteamModIds = steamModIds ?? new ulong[0];

                // Reset enabled state
                allModsEnabled = false;

                // Reset modification tracking
                modsWereModified = false;

                // Create or get the GUI component
                if (instance == null)
                {
                    GameObject guiObject = new GameObject("ModCompatibilityGUI");
                    DontDestroyOnLoad(guiObject);
                    instance = guiObject.AddComponent<ModCompatibilityGUI>();
                }

                // Center the window on screen
                instance.windowRect.x = (Screen.width - instance.windowRect.width) / 2;
                instance.windowRect.y = (Screen.height - instance.windowRect.height) / 2;

                showDialog = true;

                DebugConsole.Log("[ModCompatibilityGUI] Dialog enabled");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Failed to show dialog: {ex.Message}");
            }
        }

        public static void CloseDialog()
        {
            showDialog = false;

            // If mods were modified during this session, save and restart
            if (modsWereModified)
            {
                try
                {
                    var modManager = Global.Instance?.modManager;
                    if (modManager != null)
                    {
                        DebugConsole.Log("[ModCompatibilityGUI] Mods were modified. Saving and restarting game...");
                        SaveAndRestart(modManager);
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] Error saving/restarting on close: {ex.Message}");
                }
            }

            // Close any multiplayer overlays
            try
            {
                MultiplayerOverlay.Close();
                DebugConsole.Log("[ModCompatibilityGUI] Closed MultiplayerOverlay");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error closing MultiplayerOverlay: {ex.Message}");
            }

            if (instance != null)
            {
                DestroyImmediate(instance.gameObject);
                instance = null;
            }
        }

        public static void ShowRestartNotification()
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Showing restart notification");

                // Create or get the GUI component if not already present
                if (instance == null)
                {
                    GameObject guiObject = new GameObject("ModCompatibilityGUI");
                    DontDestroyOnLoad(guiObject);
                    instance = guiObject.AddComponent<ModCompatibilityGUI>();
                }

                showRestartNotification = true;
                restartNotificationTime = Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Failed to show restart notification: {ex.Message}");
            }
        }

        void OnGUI()
        {
            // Draw restart notification if active
            if (showRestartNotification)
            {
                DrawRestartNotification();
            }

            if (!showDialog) return;

            // Dark semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Create custom style for the window
            GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f, 0.95f));
            windowStyle.border = new RectOffset(5, 5, 5, 5);

            // Main dialog window
            windowRect = GUI.Window(12345, windowRect, DrawDialogWindow, "", windowStyle);
        }

        void DrawRestartNotification()
        {
            // Check if notification should still be visible
            float elapsed = Time.realtimeSinceStartup - restartNotificationTime;
            if (elapsed > NOTIFICATION_DURATION)
            {
                showRestartNotification = false;
                return;
            }

            // Calculate fade out for last second
            float alpha = 1f;
            if (elapsed > NOTIFICATION_DURATION - 1f)
            {
                alpha = NOTIFICATION_DURATION - elapsed; // Fade from 1 to 0 in last second
            }

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

            GUI.Label(notificationRect, "Mods enabled successfully!\nPlease restart the game for changes to take effect.", textStyle);

            GUI.color = Color.white;
        }

        void DrawDialogWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = Color.red;

            GUILayout.Label("Mod Compatibility Error", headerStyle);
            GUILayout.Space(10);

            // Scroll area for content
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(280));

            // Restart required message (if all mods were enabled)
            if (allModsEnabled)
            {
                GUIStyle restartStyle = new GUIStyle(GUI.skin.label);
                restartStyle.fontSize = 16;
                restartStyle.fontStyle = FontStyle.Bold;
                restartStyle.wordWrap = true;
                restartStyle.alignment = TextAnchor.MiddleCenter;
                restartStyle.normal.textColor = Color.green;

                GUILayout.Label("All mods have been enabled!", restartStyle);
                GUILayout.Space(10);

                GUIStyle restartInstructionStyle = new GUIStyle(GUI.skin.label);
                restartInstructionStyle.fontSize = 14;
                restartInstructionStyle.fontStyle = FontStyle.Bold;
                restartInstructionStyle.wordWrap = true;
                restartInstructionStyle.alignment = TextAnchor.MiddleCenter;
                restartInstructionStyle.normal.textColor = Color.yellow;

                GUILayout.Label("Close this window to restart the game and apply changes.", restartInstructionStyle);
                GUILayout.Space(10);
            }
            // Show modification message (if mods were changed but not all enabled yet)
            else if (modsWereModified)
            {
                GUIStyle modifiedStyle = new GUIStyle(GUI.skin.label);
                modifiedStyle.fontSize = 14;
                modifiedStyle.fontStyle = FontStyle.Bold;
                modifiedStyle.wordWrap = true;
                modifiedStyle.alignment = TextAnchor.MiddleCenter;
                modifiedStyle.normal.textColor = Color.cyan;

                GUILayout.Label("Mods have been enabled. Close this window to restart the game.", modifiedStyle);
                GUILayout.Space(10);
            }
            // Reason text
            else if (!string.IsNullOrEmpty(dialogReason))
            {
                GUIStyle reasonStyle = new GUIStyle(GUI.skin.label);
                reasonStyle.fontStyle = FontStyle.Bold;
                reasonStyle.wordWrap = true;
                reasonStyle.normal.textColor = Color.white;

                GUILayout.Label(dialogReason, reasonStyle);
                GUILayout.Space(10);
            }

            // Missing mods section - only show if actually missing and not all enabled
            if (!allModsEnabled && dialogMissingMods != null && dialogMissingMods.Length > 0)
            {
                // Filter only mods that are truly not installed/enabled
                var trulyMissingMods = new List<string>();
                var installedButDisabledMods = new List<string>();

                foreach (var mod in dialogMissingMods)
                {
                    if (IsModEnabled(mod))
                    {
                        // If enabled, not really "missing"
                        DebugConsole.Log($"[ModCompatibilityGUI] Mod {mod} is enabled, ignoring from missing list");
                        continue;
                    }
                    else if (IsModInstalled(mod))
                    {
                        installedButDisabledMods.Add(mod);
                    }
                    else
                    {
                        trulyMissingMods.Add(mod);
                    }
                }

                // Mostrar apenas mods realmente em falta
                if (trulyMissingMods.Count > 0)
                {
                    DrawModSection("MISSING MODS (install these):", trulyMissingMods.ToArray(), Color.red, "Install");
                    GUILayout.Space(10);
                }

                // Mostrar mods instalados mas desabilitados separadamente
                if (installedButDisabledMods.Count > 0)
                {
                    DrawModSection("DISABLED MODS (enable these):", installedButDisabledMods.ToArray(), Color.yellow, "Enable");
                    GUILayout.Space(10);
                }
            }

            // Extra mods section (only show if no missing mods - policy permissive)
            if (dialogExtraMods != null && dialogExtraMods.Length > 0 &&
                (dialogMissingMods == null || dialogMissingMods.Length == 0) &&
                (dialogVersionMismatches == null || dialogVersionMismatches.Length == 0))
            {
                DrawInfoSection("You have extra mods (this is allowed):", dialogExtraMods);
                GUILayout.Space(10);
            }
            else if (dialogExtraMods != null && dialogExtraMods.Length > 0)
            {
                DrawModSection("EXTRA MODS (you have these):", dialogExtraMods, Color.yellow, "View");
                GUILayout.Space(10);
            }

            // Version mismatches section
            if (dialogVersionMismatches != null && dialogVersionMismatches.Length > 0)
            {
                DrawModSection("VERSION MISMATCHES (update these):", dialogVersionMismatches, Color.cyan, "Update");
                GUILayout.Space(10);
            }

            // Instructions
            GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
            instructionStyle.fontStyle = FontStyle.Italic;
            instructionStyle.wordWrap = true;
            instructionStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            if (dialogMissingMods.Length > 0 || dialogVersionMismatches.Length > 0)
            {
                GUILayout.Label("Install/disable the required mods, then try connecting again.", instructionStyle);
            }
            else if (dialogExtraMods.Length > 0)
            {
                GUILayout.Label("Connection allowed. Your extra mods shouldn't cause issues.", instructionStyle);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Action buttons section (hide if all mods enabled)
            GUILayout.BeginHorizontal();

            if (!allModsEnabled)
            {
                // Check if there are truly missing mods or just disabled ones
                bool hasTrulyMissing = false;
                bool hasDisabled = false;

                if (dialogMissingMods != null && dialogMissingMods.Length > 0)
                {
                    foreach (var mod in dialogMissingMods)
                    {
                        if (IsModEnabled(mod))
                        {
                            // Mod is enabled, ignore
                            continue;
                        }
                        else if (IsModInstalled(mod))
                        {
                            hasDisabled = true;
                        }
                        else
                        {
                            hasTrulyMissing = true;
                        }
                    }
                }

                // Install All button (only for truly missing mods)
                if (hasTrulyMissing)
                {
                    GUIStyle installAllStyle = new GUIStyle(GUI.skin.button);
                    installAllStyle.fontSize = 12;
                    installAllStyle.fontStyle = FontStyle.Bold;
                    installAllStyle.normal.textColor = Color.cyan;

                    if (GUILayout.Button("Install All", installAllStyle, GUILayout.Height(35)))
                    {
                        InstallAllMods();
                    }

                    GUILayout.Space(10);
                }

                // Enable All button (only for installed but disabled mods)
                if (hasDisabled)
                {
                    GUIStyle enableAllStyle = new GUIStyle(GUI.skin.button);
                    enableAllStyle.fontSize = 12;
                    enableAllStyle.fontStyle = FontStyle.Bold;
                    enableAllStyle.normal.textColor = Color.green;

                    if (GUILayout.Button("Enable All", enableAllStyle, GUILayout.Height(35)))
                    {
                        EnableAllMods();
                    }

                    GUILayout.Space(10);
                }

                // Note: Removed complex auto-installation system
                // Now using simple native ONI mod management instead
            } // End of !allModsEnabled check

            GUILayout.FlexibleSpace();

            // Close button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
            buttonStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Close", buttonStyle, GUILayout.Height(35)))
            {
                CloseDialog();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow();
        }

        void DrawModSection(string title, string[] mods, Color color, string buttonText)
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

                // Mod name
                GUIStyle modStyle = new GUIStyle(GUI.skin.label);
                modStyle.normal.textColor = color;
                modStyle.wordWrap = true;

                GUILayout.Label($"• {mod}", modStyle);

                GUILayout.FlexibleSpace();

                // Check if mod is installed but disabled (for missing mods)
                bool isInstalled = IsModInstalled(mod);
                bool isDisabled = isInstalled && !IsModEnabled(mod);

                if (isDisabled && title.Contains("MISSING"))
                {
                    // Enable button for disabled mods
                    GUIStyle enableButtonStyle = new GUIStyle(GUI.skin.button);
                    enableButtonStyle.fontSize = 9;
                    enableButtonStyle.normal.textColor = Color.green;

                    if (GUILayout.Button("Enable", enableButtonStyle, GUILayout.Width(55), GUILayout.Height(20)))
                    {
                        EnableMod(mod);
                    }

                    GUILayout.Space(5);
                }

                // Original action button
                GUIStyle modButtonStyle = new GUIStyle(GUI.skin.button);
                modButtonStyle.fontSize = 10;

                if (GUILayout.Button(buttonText, modButtonStyle, GUILayout.Width(70), GUILayout.Height(20)))
                {
                    HandleModAction(mod, buttonText);
                }

                GUILayout.EndHorizontal();
            }
        }

        void DrawInfoSection(string title, string[] mods)
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

        private void OpenSteamWorkshopPage(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={modId}";

                DebugConsole.Log($"[ModCompatibilityGUI] Opening Steam Workshop: {url}");

                if (SteamManager.Initialized)
                {
                    Steamworks.SteamFriends.ActivateGameOverlayToWebPage(url);
                }
                else
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Failed to open Steam page: {ex.Message}");
            }
        }

        private string ExtractModId(string modDisplayName)
        {
            if (modDisplayName.Contains(" - "))
            {
                string[] parts = modDisplayName.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string lastPart = parts[parts.Length - 1];
                    if (System.Text.RegularExpressions.Regex.IsMatch(lastPart, @"^\d+$"))
                    {
                        return lastPart;
                    }
                }
            }

            var match = System.Text.RegularExpressions.Regex.Match(modDisplayName, @"\d+");
            return match.Success ? match.Value : modDisplayName;
        }

        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        // Mod activation functionality
        private bool HasDisabledMods()
        {
            if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                return false;

            try
            {
                foreach (var mod in dialogMissingMods)
                {
                    if (IsModInstalled(mod) && !IsModEnabled(mod))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking disabled mods: {ex.Message}");
            }

            return false;
        }

        private bool IsModInstalled(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;
                if (modManager == null) return false;

                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null)
                    {
                        // Check multiple ID formats for Steam mods
                        string defaultId = mod.label.defaultStaticID;
                        string labelId = mod.label.id;

                        // Try exact match with extracted ID
                        if (defaultId == modId || labelId == modId)
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Found installed mod: {modDisplayName} -> {defaultId}");
                            return true;
                        }

                        // Try exact match with original display name
                        if (defaultId == modDisplayName || labelId == modDisplayName)
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Found installed mod: {modDisplayName} -> {defaultId}");
                            return true;
                        }

                        // For Steam mods, try numeric ID match
                        if (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId)))
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Found installed Steam mod: {modDisplayName} -> {defaultId}");
                            return true;
                        }
                    }
                }

                DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod not found: {modDisplayName} (extracted: {modId})");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking if mod is installed: {ex.Message}");
            }

            return false;
        }

        private bool IsModEnabled(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;
                if (modManager == null) return false;

                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null)
                    {
                        // Check multiple ID formats for Steam mods
                        string defaultId = mod.label.defaultStaticID;
                        string labelId = mod.label.id;

                        // Try exact match with extracted ID
                        if (defaultId == modId || labelId == modId)
                        {
                            return mod.IsEnabledForActiveDlc();
                        }

                        // Try exact match with original display name
                        if (defaultId == modDisplayName || labelId == modDisplayName)
                        {
                            return mod.IsEnabledForActiveDlc();
                        }

                        // For Steam mods, try numeric ID match
                        if (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId)))
                        {
                            return mod.IsEnabledForActiveDlc();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking if mod is enabled: {ex.Message}");
            }

            return false;
        }

        private void CheckAllModsEnabled()
        {
            try
            {
                if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                {
                    allModsEnabled = false;
                    return;
                }

                // Check if all missing mods are now enabled
                bool allEnabled = true;
                foreach (var mod in dialogMissingMods)
                {
                    if (!IsModEnabled(mod))
                    {
                        allEnabled = false;
                        break;
                    }
                }

                if (allEnabled)
                {
                    allModsEnabled = true;
                    DebugConsole.Log("[ModCompatibilityGUI] All required mods are now enabled! Restart required.");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking all mods enabled: {ex.Message}");
            }
        }

        private void EnableMod(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;

                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] ModManager not available");
                    OpenSteamWorkshopPage(modDisplayName);
                    return;
                }

                // Search for the mod using robust ID matching for Steam mods
                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null)
                    {
                        string defaultId = mod.label.defaultStaticID;
                        string labelId = mod.label.id;
                        bool foundMod = false;

                        // Check multiple ID formats for Steam mods
                        if (defaultId == modId || labelId == modId ||
                            defaultId == modDisplayName || labelId == modDisplayName ||
                            (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId))))
                        {
                            foundMod = true;
                            DebugConsole.Log($"[ModCompatibilityGUI] Found mod to enable: {modDisplayName} -> {defaultId}");
                        }

                        if (foundMod)
                        {
                            // Check if already enabled using proper ONI method
                            if (mod.IsEnabledForActiveDlc())
                            {
                                DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} was already enabled");
                                CheckAllModsEnabled();
                                return;
                            }

                            // Check if mod is compatible
                            if (mod.available_content == 0)
                            {
                                DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod {modDisplayName} is not compatible - opening Steam page");
                                OpenSteamWorkshopPage(modDisplayName);
                                return;
                            }

                            try
                            {
                                // Enable the mod using proper ONI API (follows SaveGameModLoader pattern)
                                mod.SetEnabledForActiveDlc(true);

                                DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} enabled successfully!");

                                // Mark that mods were modified (restart will happen when closing dialog)
                                modsWereModified = true;

                                // Check if all mods are now enabled
                                CheckAllModsEnabled();

                                return;
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error enabling mod {modDisplayName}: {ex.Message}");
                                OpenSteamWorkshopPage(modDisplayName);
                                return;
                            }
                        }
                    }
                }

                DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod {modDisplayName} not found in list");
                OpenSteamWorkshopPage(modDisplayName);
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in EnableMod: {ex.Message}");
                OpenSteamWorkshopPage(modDisplayName);
            }
        }

        private void HandleModAction(string modDisplayName, string buttonText)
        {
            try
            {
                if (buttonText == "Enable")
                {
                    // Enable installed but disabled mod using native ONI system
                    DebugConsole.Log($"[ModCompatibilityGUI] Attempting to enable mod: {modDisplayName}");
                    EnableMod(modDisplayName);
                }
                else
                {
                    // All other buttons (Install, View, Update) - open Steam Workshop page
                    // User must manually install mods via Steam Workshop
                    DebugConsole.Log($"[ModCompatibilityGUI] Opening Steam Workshop for mod: {modDisplayName}");
                    OpenSteamWorkshopPage(modDisplayName);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error processing mod action {modDisplayName}: {ex.Message}");
                // Fallback to open Steam Workshop page
                OpenSteamWorkshopPage(modDisplayName);
            }
        }

        private void InstallAllMods()
        {
            try
            {
                if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                    return;

                DebugConsole.Log($"[ModCompatibilityGUI] Starting installation of {dialogMissingMods.Length} mods...");

                // Extract mod IDs
                List<string> modIds = new List<string>();
                foreach (var mod in dialogMissingMods)
                {
                    string modId = ExtractModId(mod);
                    if (!string.IsNullOrEmpty(modId))
                    {
                        modIds.Add(modId);
                    }
                }

                if (modIds.Count == 0)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] No valid mod IDs found");
                    return;
                }

                // Use WorkshopInstaller to install all
                WorkshopInstaller.Instance.InstallMultipleItems(
                    modIds.ToArray(),
                    onProgress: (completed, total) => {
                        DebugConsole.Log($"[ModCompatibilityGUI] Installation progress: {completed}/{total}");
                    },
                    onComplete: installedPaths => {
                        DebugConsole.Log($"[ModCompatibilityGUI] Batch installation completed! {installedPaths.Length} mods processed");

                        // Try to activate all installed mods
                        int activatedCount = 0;
                        for (int i = 0; i < modIds.Count && i < installedPaths.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(installedPaths[i]))
                            {
                                if (WorkshopInstaller.Instance.ActivateInstalledMod(modIds[i], installedPaths[i]))
                                {
                                    activatedCount++;
                                }
                            }
                        }

                        DebugConsole.Log($"[ModCompatibilityGUI] {activatedCount} mods activated automatically");

                        if (activatedCount < modIds.Count)
                        {
                            DebugConsole.Log("[ModCompatibilityGUI] Some mods may need manual activation or game restart");
                        }
                    },
                    onError: error => {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in batch installation: {error}");

                        // Fallback: open first Steam Workshop page
                        if (dialogMissingMods.Length > 0)
                        {
                            OpenSteamWorkshopPage(dialogMissingMods[0]);
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in InstallAllMods: {ex.Message}");
            }
        }

        private void EnableAllMods()
        {
            try
            {
                if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                    return;

                var modManager = Global.Instance?.modManager;
                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] ModManager not available");
                    // Fallback to open Steam Workshop
                    if (dialogMissingMods.Length > 0)
                    {
                        OpenSteamWorkshopPage(dialogMissingMods[0]);
                    }
                    return;
                }

                int enabledCount = 0;
                int notFoundCount = 0;

                foreach (var modDisplayName in dialogMissingMods)
                {
                    if (IsModInstalled(modDisplayName) && !IsModEnabled(modDisplayName))
                    {
                        string modId = ExtractModId(modDisplayName);
                        bool modFound = false;

                        foreach (var mod in modManager.mods)
                        {
                            if (mod?.label != null)
                            {
                                string defaultId = mod.label.defaultStaticID;
                                string labelId = mod.label.id;

                                // Use robust Steam mod matching (same as other functions)
                                if (defaultId == modId || labelId == modId ||
                                    defaultId == modDisplayName || labelId == modDisplayName ||
                                    (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId))))
                                {
                                    try
                                    {
                                        // Check if mod is compatible
                                        if (mod.available_content == 0)
                                        {
                                            DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod {modDisplayName} is not compatible - skipping");
                                            modFound = true;
                                            break;
                                        }

                                        // Enable using proper ONI API (follows SaveGameModLoader pattern)
                                        mod.SetEnabledForActiveDlc(true);
                                        enabledCount++;
                                        modFound = true;
                                        DebugConsole.Log($"[ModCompatibilityGUI] Enabled: {modDisplayName} -> {defaultId}");
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Error enabling {modDisplayName}: {ex.Message}");
                                    }
                                }
                            }
                        }

                        if (!modFound)
                        {
                            notFoundCount++;
                            DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod not found: {modDisplayName}");
                        }
                    }
                }

                if (enabledCount > 0)
                {
                    try
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] {enabledCount} mods enabled successfully!");

                        if (notFoundCount > 0)
                        {
                            DebugConsole.LogWarning($"[ModCompatibilityGUI] {notFoundCount} mods were not found in the list");
                        }

                        // Mark that mods were modified (restart will happen when closing dialog)
                        modsWereModified = true;

                        // Check if all mods are now enabled
                        CheckAllModsEnabled();
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Error enabling mods: {ex.Message}");
                    }
                }
                else
                {
                    DebugConsole.Log("[ModCompatibilityGUI] No disabled mods found to enable");

                    // If none could be enabled, open Steam Workshop as fallback
                    if (dialogMissingMods.Length > 0)
                    {
                        DebugConsole.Log("[ModCompatibilityGUI] Opening Steam Workshop as fallback");
                        OpenSteamWorkshopPage(dialogMissingMods[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in EnableAllMods: {ex.Message}");
            }
        }

        private void InstallAndSyncMods()
        {
            try
            {
                DebugConsole.Log($"[ModCompatibilityGUI] Starting Install & Sync for {dialogSteamModIds.Length} Steam mods...");

                // Converte Steam IDs para array de strings
                string[] steamModIdStrings = new string[dialogSteamModIds.Length];
                for (int i = 0; i < dialogSteamModIds.Length; i++)
                {
                    steamModIdStrings[i] = dialogSteamModIds[i].ToString();
                }

                // Usa WorkshopInstaller para instalar e ativar mods automaticamente
                WorkshopInstaller.Instance.InstallMultipleItems(
                    steamModIdStrings,
                    onProgress: (completed, total) =>
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] Progresso da instalação: {completed}/{total}");
                    },
                    onComplete: installedPaths =>
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] Todos os {installedPaths.Length} mods foram instalados!");

                        // Agora ativa os mods instalados
                        foreach (var steamIdString in steamModIdStrings)
                        {
                            WorkshopInstaller.Instance.ActivateInstalledMod(steamIdString, "");
                        }

                        // Ajusta mods locais (habilita requeridos, desabilita extras)
                        AdjustLocalMods();

                        // Fecha o diálogo
                        CloseDialog();

                        // Reinicia o jogo
                        DebugConsole.Log("[ModCompatibilityGUI] Reiniciando o jogo para aplicar mudanças de mods...");
                        App.instance.Restart();
                    },
                    onError: error =>
                    {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro durante instalação: {error}");
                        DebugConsole.LogWarning("[ModCompatibilityGUI] Continuando com ajuste de mods locais...");

                        // Mesmo com erro, tenta ajustar mods locais
                        AdjustLocalMods();
                        CloseDialog();
                        App.instance.Restart();
                    }
                );
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro em InstallAndSyncMods: {ex.Message}");
            }
        }

        private void AdjustLocalMods()
        {
            var modManager = Global.Instance?.modManager;
            if (modManager == null)
            {
                DebugConsole.LogWarning("[ModCompatibilityGUI] ModManager não disponível");
                return;
            }

            try
            {
                // Constrói conjunto de IDs de mods requeridos
                var requiredModIds = new HashSet<string>();
                if (dialogMissingMods != null)
                {
                    foreach (var mod in dialogMissingMods)
                    {
                        string modId = ExtractModId(mod);
                        if (!string.IsNullOrEmpty(modId))
                        {
                            requiredModIds.Add(modId);
                        }
                    }
                }

                // Habilita/desabilita mods conforme necessário
                int enabledCount = 0;
                int disabledCount = 0;
                foreach (var mod in modManager.mods)
                {
                    if (mod?.label == null) continue;

                    bool shouldBeEnabled = requiredModIds.Contains(mod.label.id);
                    bool isEnabled = mod.IsEnabledForActiveDlc();

                    if (shouldBeEnabled != isEnabled)
                    {
                        mod.SetEnabledForActiveDlc(shouldBeEnabled);
                        if (shouldBeEnabled)
                        {
                            enabledCount++;
                            DebugConsole.Log($"[ModCompatibilityGUI] Habilitado: {mod.label.title}");
                        }
                        else if (dialogExtraMods != null && dialogExtraMods.Any(e => ExtractModId(e) == mod.label.id))
                        {
                            disabledCount++;
                            DebugConsole.Log($"[ModCompatibilityGUI] Desabilitado: {mod.label.title}");
                        }
                    }
                }

                DebugConsole.Log($"[ModCompatibilityGUI] Mods ajustados: {enabledCount} habilitados, {disabledCount} desabilitados");

                // Salva configuração de mods
                modManager.Save();
                DebugConsole.Log("[ModCompatibilityGUI] Configuração de mods salva");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro ao ajustar mods: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves mod configuration and triggers automatic game restart
        /// Based on SaveGameModLoader.AutoRestart() pattern
        /// </summary>
        private static void SaveAndRestart(KMod.Manager modManager)
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Saving mod configuration and triggering restart...");

                // Save mod configuration - this should trigger automatic restart
                modManager.Save();

                // Additional restart trigger if needed (following SaveGameModLoader pattern)
                try
                {
                    // Force restart via App.instance if modManager.Save() doesn't work
                    if (App.instance != null)
                    {
                        DebugConsole.Log("[ModCompatibilityGUI] Triggering manual restart via App.instance...");
                        App.instance.Restart();
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] Manual restart failed: {ex.Message}");
                    DebugConsole.Log("[ModCompatibilityGUI] Please restart the game manually to apply mod changes.");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in SaveAndRestart: {ex.Message}");
                DebugConsole.Log("[ModCompatibilityGUI] Please restart the game manually to apply mod changes.");
            }
        }
    }
}