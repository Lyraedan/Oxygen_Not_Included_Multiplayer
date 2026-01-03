using UnityEngine;
using System;
using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Managers;

namespace ONI_MP.Menus
{
    /// <summary>
    /// Professional confirmation dialog shown when applying mod changes
    /// Shows exactly which mods were activated and requires user confirmation
    /// </summary>
    public class ModApplyConfirmationDialog : MonoBehaviour
    {
        private static ModApplyConfirmationDialog instance;
        private static bool showDialog = false;
        private static List<string> activatedMods = new List<string>();
        private static List<string> deactivatedMods = new List<string>();
        private static Vector2 scrollPosition = Vector2.zero;
        private static Rect windowRect = new Rect(0, 0, 650, 500);

        /// <summary>
        /// Shows the apply confirmation dialog with the list of modified mods
        /// </summary>
        public static void ShowConfirmation(List<string> activated, List<string> deactivated = null)
        {
            try
            {
                DebugConsole.Log("[ModApplyConfirmationDialog] Showing apply confirmation dialog");

                // Store the lists
                activatedMods = activated ?? new List<string>();
                deactivatedMods = deactivated ?? new List<string>();

                // Create or get the dialog component
                if (instance == null)
                {
                    GameObject dialogObject = new GameObject("ModApplyConfirmationDialog");
                    DontDestroyOnLoad(dialogObject);
                    instance = dialogObject.AddComponent<ModApplyConfirmationDialog>();
                }

                // Center the window on screen
                windowRect.x = (Screen.width - windowRect.width) / 2;
                windowRect.y = (Screen.height - windowRect.height) / 2;

                showDialog = true;
                scrollPosition = Vector2.zero;

                DebugConsole.Log($"[ModApplyConfirmationDialog] Showing {activatedMods.Count} activated mods");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModApplyConfirmationDialog] Error showing confirmation: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the confirmation dialog
        /// </summary>
        public static void CloseDialog()
        {
            showDialog = false;

            if (instance != null)
            {
                DestroyImmediate(instance.gameObject);
                instance = null;
            }
        }

        void OnGUI()
        {
            if (!showDialog) return;

            // Dark semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Create custom window style
            GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = CreateColorTexture(new Color(0.15f, 0.15f, 0.15f, 0.95f));
            windowStyle.border = new RectOffset(10, 10, 10, 10);

            // Main dialog window
            windowRect = GUI.Window(98765, windowRect, DrawConfirmationWindow, "", windowStyle);
        }

        void DrawConfirmationWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Header with icon and title
            GUILayout.BeginHorizontal();

            // Success icon (green checkmark representation)
            GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 24;
            iconStyle.fontStyle = FontStyle.Bold;
            iconStyle.normal.textColor = Color.green;
            iconStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label("✓", iconStyle, GUILayout.Width(40), GUILayout.Height(40));

            // Title and summary
            GUILayout.BeginVertical();

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 18;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.CHANGES_APPLIED_TITLE, titleStyle);

            GUIStyle summaryStyle = new GUIStyle(GUI.skin.label);
            summaryStyle.fontSize = 14;
            summaryStyle.normal.textColor = Color.gray;

            string summaryText = GenerateSummaryText();
            GUILayout.Label(summaryText, summaryStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            // Scroll area for mod details
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

            // Activated mods section
            if (activatedMods.Count > 0)
            {
                DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ACTIVATED_MODS_SECTION, activatedMods, Color.green, "✓");
                GUILayout.Space(10);
            }

            // Deactivated mods section
            if (deactivatedMods.Count > 0)
            {
                DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.DEACTIVATED_MODS_SECTION, deactivatedMods, new Color(1f, 0.6f, 0f), "✗");
                GUILayout.Space(10);
            }

            // Restart requirement message
            DrawRestartRequirement();

            GUILayout.EndScrollView();

            GUILayout.Space(15);

            // Action buttons
            DrawActionButtons();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow();
        }

        /// <summary>
        /// Generates summary text for the header
        /// </summary>
        private string GenerateSummaryText()
        {
            int totalChanges = activatedMods.Count + deactivatedMods.Count;

            if (totalChanges == 1)
            {
                if (activatedMods.Count == 1)
                    return MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ONE_MOD_ACTIVATED;
                else
                    return MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ONE_MOD_DEACTIVATED;
            }
            else if (activatedMods.Count > 0 && deactivatedMods.Count == 0)
            {
                return string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.MULTIPLE_MODS_ACTIVATED, activatedMods.Count);
            }
            else if (deactivatedMods.Count > 0 && activatedMods.Count == 0)
            {
                return string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.MULTIPLE_MODS_DEACTIVATED, deactivatedMods.Count);
            }
            else
            {
                return string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.MULTIPLE_MODS_MODIFIED, totalChanges);
            }
        }

        /// <summary>
        /// Draws a section showing a list of mods
        /// </summary>
        private void DrawModSection(string title, List<string> mods, Color color, string icon)
        {
            // Section title
            GUIStyle sectionTitleStyle = new GUIStyle(GUI.skin.label);
            sectionTitleStyle.fontSize = 16;
            sectionTitleStyle.fontStyle = FontStyle.Bold;
            sectionTitleStyle.normal.textColor = color;

            GUILayout.Label(title, sectionTitleStyle);
            GUILayout.Space(5);

            // Mod list
            foreach (var mod in mods)
            {
                GUILayout.BeginHorizontal();

                // Status icon
                GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
                iconStyle.fontSize = 12;
                iconStyle.fontStyle = FontStyle.Bold;
                iconStyle.normal.textColor = color;
                iconStyle.alignment = TextAnchor.MiddleCenter;

                GUILayout.Label(icon, iconStyle, GUILayout.Width(20));

                // Mod name
                GUIStyle modNameStyle = new GUIStyle(GUI.skin.label);
                modNameStyle.fontSize = 14;
                modNameStyle.normal.textColor = Color.white;
                modNameStyle.wordWrap = true;

                GUILayout.Label(mod, modNameStyle);

                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Draws the restart requirement message
        /// </summary>
        private void DrawRestartRequirement()
        {
            // Warning box background
            Rect warningRect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
            GUI.color = new Color(1f, 0.8f, 0f, 0.2f); // Yellow background
            GUI.Box(warningRect, "");
            GUI.color = Color.white;

            GUILayout.Space(-60); // Overlap the box we just drew

            GUILayout.BeginVertical();
            GUILayout.Space(10);

            // Warning icon and text
            GUILayout.BeginHorizontal();

            GUIStyle warningIconStyle = new GUIStyle(GUI.skin.label);
            warningIconStyle.fontSize = 18;
            warningIconStyle.fontStyle = FontStyle.Bold;
            warningIconStyle.normal.textColor = Color.yellow;
            warningIconStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label("⚠", warningIconStyle, GUILayout.Width(30));

            GUIStyle warningTextStyle = new GUIStyle(GUI.skin.label);
            warningTextStyle.fontSize = 14;
            warningTextStyle.fontStyle = FontStyle.Bold;
            warningTextStyle.normal.textColor = Color.yellow;
            warningTextStyle.wordWrap = true;

            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.GAME_RESTART_REQUIRED_MESSAGE, warningTextStyle);

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the action buttons
        /// </summary>
        private void DrawActionButtons()
        {
            GUILayout.BeginHorizontal();

            // Restart Now button (prominent)
            GUIStyle restartNowStyle = new GUIStyle(GUI.skin.button);
            restartNowStyle.fontSize = 16;
            restartNowStyle.fontStyle = FontStyle.Bold;
            restartNowStyle.normal.textColor = Color.green;

            if (GUILayout.Button(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.RESTART_NOW, restartNowStyle, GUILayout.Height(40), GUILayout.MinWidth(150)))
            {
                DebugConsole.Log("[ModApplyConfirmationDialog] User chose to restart now");
                CloseDialog();
                ModRestartManager.TriggerGameRestart();
            }

            GUILayout.Space(20);

            // Restart Later button (secondary)
            GUIStyle restartLaterStyle = new GUIStyle(GUI.skin.button);
            restartLaterStyle.fontSize = 14;
            restartLaterStyle.normal.textColor = Color.gray;

            if (GUILayout.Button(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.RESTART_LATER, restartLaterStyle, GUILayout.Height(40), GUILayout.MinWidth(150)))
            {
                DebugConsole.Log("[ModApplyConfirmationDialog] User chose to restart later");
                CloseDialog();

                // Show a notification reminder
                ModRestartManager.ShowRestartNotification();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates a colored texture for UI elements
        /// </summary>
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}