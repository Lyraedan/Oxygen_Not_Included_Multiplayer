using ONI_MP.DebugTools;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ONI_MP.Menus
{
    public static class ModCompatibilityPopup
    {
        private static GameObject currentPopup;

        public static void ShowIncompatibilityError(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityPopup] Showing mod compatibility error dialog...");

                // Use new dynamic IMGUI approach - much simpler!
                ModCompatibilityGUI.ShowIncompatibilityError(reason, missingMods, extraMods, versionMismatches);

                DebugConsole.Log("[ModCompatibilityPopup] Mod compatibility message displayed.");
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityPopup] Error showing dialog: {ex.Message}");
                // Fallback to MultiplayerOverlay approach
                try
                {
                    ShowViaMultiplayerOverlay(reason, missingMods, extraMods, versionMismatches);
                }
                catch (System.Exception ex2)
                {
                    DebugConsole.LogWarning($"[ModCompatibilityPopup] Fallback overlay failed: {ex2.Message}");
                    // Ultimate fallback: just log the message
                    ShowViaNotification(reason, missingMods, extraMods, versionMismatches);
                }
            }
        }

        private static void ShowViaMultiplayerOverlay(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            try
            {
                // Build a detailed message for the overlay with mod IDs
                var detailedMessage = "MOD COMPATIBILITY ERROR!\n\n";

                if (missingMods != null && missingMods.Length > 0)
                {
                    detailedMessage += $"MISSING MODS (install these):\n";
                    foreach (var mod in missingMods)
                    {
                        detailedMessage += $"• {mod}\n";
                    }
                    detailedMessage += "\n";
                }

                if (extraMods != null && extraMods.Length > 0)
                {
                    detailedMessage += $"EXTRA MODS (disable these):\n";
                    foreach (var mod in extraMods)
                    {
                        detailedMessage += $"• {mod}\n";
                    }
                    detailedMessage += "\n";
                }

                if (versionMismatches != null && versionMismatches.Length > 0)
                {
                    detailedMessage += $"VERSION MISMATCHES:\n";
                    foreach (var mod in versionMismatches)
                    {
                        detailedMessage += $"• {mod}\n";
                    }
                    detailedMessage += "\n";
                }

                detailedMessage += "Install/disable mods, then reconnect.\nPress ESC to close.";

                // Show the error using MultiplayerOverlay with detailed mod info
                MultiplayerOverlay.Show(detailedMessage);

                // Also log detailed info to console
                ShowViaNotification(reason, missingMods, extraMods, versionMismatches);

                DebugConsole.Log("[ModCompatibilityPopup] Shown via MultiplayerOverlay with detailed mod list.");
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityPopup] MultiplayerOverlay approach failed: {ex.Message}");
                // Fallback to console-only
                ShowViaNotification(reason, missingMods, extraMods, versionMismatches);
            }
        }

        private static void ShowViaKModalScreen(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            string title = "Mod Compatibility Error";
            string message = BuildMessage(reason, missingMods, extraMods, versionMismatches);

            // Create a modal dialog similar to how the game handles critical errors
            var modalScreen = Util.KInstantiateUI(ScreenPrefabs.Instance.ConfirmDialogScreen.gameObject,
                GameScreenManager.Instance.ssOverlayCanvas.gameObject, true);

            var confirmDialog = modalScreen.GetComponent<ConfirmDialogScreen>();
            confirmDialog.PopupConfirmDialog(
                message,
                () => { }, // OK callback
                null,      // Cancel callback (none)
                null,      // Configure buttons callback
                null,      // On screen deactivate
                title,     // Title
                null       // Confirm text
            );

            DebugConsole.Log("[ModCompatibilityPopup] KModalScreen dialog shown.");
        }

        private static void ShowViaInfoDialog(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            string title = "Mod Compatibility Error";
            string message = BuildMessage(reason, missingMods, extraMods, versionMismatches);

            // Try using InfoDialogScreen instead
            var infoDialog = Util.KInstantiateUI(ScreenPrefabs.Instance.InfoDialogScreen.gameObject,
                GameScreenManager.Instance.ssOverlayCanvas.gameObject, true);

            var infoScreen = infoDialog.GetComponent<InfoDialogScreen>();
            infoScreen.SetHeader(title)
                     .AddDefaultOK()
                     .AddPlainText(message);

            DebugConsole.Log("[ModCompatibilityPopup] InfoDialog shown.");
        }

        private static void ShowViaNotification(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            string message = $"MOD COMPATIBILITY ERROR: {reason}";

            // Add critical mods info
            if (missingMods != null && missingMods.Length > 0)
            {
                message += $"\nMissing: {missingMods.Length} mods";
            }
            if (extraMods != null && extraMods.Length > 0)
            {
                message += $"\nExtra: {extraMods.Length} mods";
            }

            // Just log the critical message - this is the safest fallback
            DebugConsole.Log($"[ModCompatibilityPopup] CRITICAL: {message}");

            // Show detailed breakdown
            DebugConsole.Log("[ModCompatibilityPopup] ========================================");
            DebugConsole.Log("[ModCompatibilityPopup] MOD COMPATIBILITY CHECK FAILED");
            DebugConsole.Log("[ModCompatibilityPopup] ========================================");

            if (missingMods != null && missingMods.Length > 0)
            {
                DebugConsole.Log("[ModCompatibilityPopup] MISSING MODS (install these):");
                foreach (var mod in missingMods)
                {
                    DebugConsole.Log($"[ModCompatibilityPopup]   • {mod}");
                }
            }

            if (extraMods != null && extraMods.Length > 0)
            {
                DebugConsole.Log("[ModCompatibilityPopup] EXTRA MODS (disable these):");
                foreach (var mod in extraMods)
                {
                    DebugConsole.Log($"[ModCompatibilityPopup]   • {mod}");
                }
            }

            if (versionMismatches != null && versionMismatches.Length > 0)
            {
                DebugConsole.Log("[ModCompatibilityPopup] VERSION MISMATCHES (update these):");
                foreach (var mod in versionMismatches)
                {
                    DebugConsole.Log($"[ModCompatibilityPopup]   • {mod}");
                }
            }

            DebugConsole.Log("[ModCompatibilityPopup] ========================================");
            DebugConsole.Log("[ModCompatibilityPopup] Please check console for mod details.");
            DebugConsole.Log("[ModCompatibilityPopup] Press Shift+F1 to open debug console.");
        }

        private static string BuildMessage(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            var message = $"{reason}\n\n";

            if (missingMods != null && missingMods.Length > 0)
            {
                message += "MISSING MODS (Install these):\n";
                foreach (var mod in missingMods)
                {
                    message += $"• {mod}\n";
                }
                message += "\n";
            }

            if (extraMods != null && extraMods.Length > 0)
            {
                message += "EXTRA MODS (Disable these):\n";
                foreach (var mod in extraMods)
                {
                    message += $"• {mod}\n";
                }
                message += "\n";
            }

            if (versionMismatches != null && versionMismatches.Length > 0)
            {
                message += "VERSION MISMATCHES (Update these):\n";
                foreach (var mod in versionMismatches)
                {
                    message += $"• {mod}\n";
                }
                message += "\n";
            }

            message += "Please ensure your mods match the host's configuration.";
            return message;
        }

        public static void Close()
        {
            // Not needed anymore since we use the game's dialog system
            DebugConsole.Log("[ModCompatibilityPopup] Close called - using game's dialog system.");
        }
    }
}