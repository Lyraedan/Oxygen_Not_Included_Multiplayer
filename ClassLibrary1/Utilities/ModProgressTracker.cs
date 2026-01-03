using System.Collections;
using UnityEngine;
using ONI_MP.DebugTools;

namespace ONI_MP.Utilities
{
    /// <summary>
    /// Manages installation progress tracking and UI updates
    /// </summary>
    public static class ModProgressTracker
    {
        // Installation progress tracking
        private static bool isInstalling = false;
        private static float installProgress = 0f;
        private static string installStatusMessage = "";
        private static int totalModsToInstall = 0;
        private static int completedModInstalls = 0;

        /// <summary>
        /// Gets whether installation is currently in progress
        /// </summary>
        public static bool IsInstalling => isInstalling;

        /// <summary>
        /// Gets the current installation progress (0.0 to 1.0)
        /// </summary>
        public static float InstallProgress => installProgress;

        /// <summary>
        /// Gets the current status message
        /// </summary>
        public static string InstallStatusMessage => installStatusMessage;

        /// <summary>
        /// Gets the total number of mods to install
        /// </summary>
        public static int TotalModsToInstall => totalModsToInstall;

        /// <summary>
        /// Gets the number of completed mod installations
        /// </summary>
        public static int CompletedModInstalls => completedModInstalls;

        /// <summary>
        /// Starts installation progress tracking
        /// </summary>
        /// <param name="totalMods">Total number of mods to install</param>
        /// <param name="statusMessage">Initial status message</param>
        public static void StartInstallationProgress(int totalMods, string statusMessage)
        {
            isInstalling = true;
            installProgress = 0f;
            installStatusMessage = statusMessage;
            totalModsToInstall = totalMods;
            completedModInstalls = 0;

        }

        /// <summary>
        /// Updates installation progress
        /// </summary>
        /// <param name="completed">Number of completed installations</param>
        /// <param name="total">Total number of installations</param>
        /// <param name="statusMessage">Current status message</param>
        public static void UpdateInstallationProgress(int completed, int total, string statusMessage)
        {
            if (!isInstalling) return;

            completedModInstalls = completed;
            totalModsToInstall = total;
            installProgress = total > 0 ? (float)completed / total : 1f;
            installStatusMessage = statusMessage;

        }

        /// <summary>
        /// Updates progress for a single mod step
        /// </summary>
        /// <param name="statusMessage">Status message for this step</param>
        public static void UpdateProgressStep(string statusMessage)
        {
            if (!isInstalling) return;

            installStatusMessage = statusMessage;
        }

        /// <summary>
        /// Increments the completed mod count and updates progress
        /// </summary>
        /// <param name="statusMessage">Status message for this completion</param>
        public static void IncrementProgress(string statusMessage)
        {
            if (!isInstalling) return;

            completedModInstalls++;
            installProgress = totalModsToInstall > 0 ? (float)completedModInstalls / totalModsToInstall : 1f;
            installStatusMessage = statusMessage;

        }

        /// <summary>
        /// Completes installation progress with final message
        /// </summary>
        /// <param name="finalMessage">Final completion message</param>
        public static void CompleteInstallationProgress(string finalMessage)
        {
            if (!isInstalling) return;

            installProgress = 1f;
            installStatusMessage = finalMessage;

        }

        /// <summary>
        /// Hides installation progress after a delay
        /// </summary>
        /// <param name="delay">Delay in seconds before hiding</param>
        /// <param name="behaviour">MonoBehaviour to run coroutine on</param>
        public static void HideProgressAfterDelay(float delay, MonoBehaviour behaviour)
        {
            if (behaviour != null)
            {
                behaviour.StartCoroutine(HideProgressCoroutine(delay));
            }
            else
            {
                // Immediate hide if no behaviour provided
                HideProgress();
            }
        }

        /// <summary>
        /// Immediately hides the progress tracking
        /// </summary>
        public static void HideProgress()
        {
            isInstalling = false;
            installProgress = 0f;
            installStatusMessage = "";
            totalModsToInstall = 0;
            completedModInstalls = 0;

        }

        /// <summary>
        /// Coroutine to hide progress after delay
        /// </summary>
        private static IEnumerator HideProgressCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideProgress();
        }

        /// <summary>
        /// Gets a formatted progress text string
        /// </summary>
        /// <returns>Formatted progress text</returns>
        public static string GetProgressText()
        {
            if (totalModsToInstall > 0)
            {
                return $"{completedModInstalls}/{totalModsToInstall} ({(installProgress * 100):F0}%)";
            }
            else
            {
                return $"{(installProgress * 100):F0}%";
            }
        }

        /// <summary>
        /// Resets all progress tracking
        /// </summary>
        public static void Reset()
        {
            isInstalling = false;
            installProgress = 0f;
            installStatusMessage = "";
            totalModsToInstall = 0;
            completedModInstalls = 0;

            DebugConsole.Log("[ModProgressTracker] Progress tracking reset");
        }

        /// <summary>
        /// Gets detailed status for debugging
        /// </summary>
        public static void LogStatus()
        {
            DebugConsole.Log($"[ModProgressTracker] Status: Installing={isInstalling}, Progress={installProgress:F2}, " +
                           $"Completed={completedModInstalls}, Total={totalModsToInstall}, Message='{installStatusMessage}'");
        }
    }
}