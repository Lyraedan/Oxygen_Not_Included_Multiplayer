using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using ONI_MP.DebugTools;
using ONI_MP.Utilities;

namespace ONI_MP.Managers
{
    /// <summary>
    /// Manages mod states, caching, and verification to improve performance
    /// </summary>
    public static class ModStateManager
    {
        // Mod state verification cache to reduce excessive checks (3 second intervals)
        private static Dictionary<string, bool> modInstalledCache = new Dictionary<string, bool>();
        private static Dictionary<string, bool> modEnabledCache = new Dictionary<string, bool>();
        private static Dictionary<string, float> modCacheTime = new Dictionary<string, float>();
        private static readonly float MOD_CACHE_SECONDS = 3f; // Verify mod status only every 3 seconds

        // Individual mod state tracking for dynamic buttons
        private static Dictionary<string, ModButtonState> modStates = new Dictionary<string, ModButtonState>();

        // Cache for missing mods to reduce spam
        private static HashSet<string> missingModsCache = new HashSet<string>();
        private static float lastCacheUpdate = 0f;
        private static readonly float CACHE_REFRESH_SECONDS = 10f; // Refresh cache every 10 seconds

        /// <summary>
        /// Enum for mod button states (Subscribe -> Progress -> Enable -> Disable)
        /// </summary>
        public enum ModButtonState
        {
            Subscribe,    // Mod not subscribed - "Subscribe to Mod X"
            Subscribing,  // During subscription - "Subscribing..."
            Enable,       // Mod subscribed but disabled - "Enable Mod X"
            Disable       // Mod enabled - "Disable Mod X"
        }

        /// <summary>
        /// Checks if a mod is installed using cached verification
        /// </summary>
        public static bool IsModInstalled(string modDisplayName)
        {
            try
            {
                // Check temporal cache first to reduce excessive checks
                float currentTime = Time.realtimeSinceStartup;
                string cacheKey = $"installed_{modDisplayName}";

                if (modCacheTime.ContainsKey(cacheKey) &&
                    (currentTime - modCacheTime[cacheKey]) < MOD_CACHE_SECONDS)
                {
                    // Return cached result if within time window
                    return modInstalledCache.ContainsKey(cacheKey) ? modInstalledCache[cacheKey] : false;
                }

                // Perform actual mod lookup
                bool isInstalled = CheckModInstalled(modDisplayName);

                // Update cache
                modInstalledCache[cacheKey] = isInstalled;
                modCacheTime[cacheKey] = currentTime;

                return isInstalled;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModStateManager] Error checking if mod is installed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a mod is enabled using cached verification
        /// </summary>
        public static bool IsModEnabled(string modDisplayName)
        {
            try
            {
                // Check temporal cache first to reduce excessive checks
                float currentTime = Time.realtimeSinceStartup;
                string cacheKey = $"enabled_{modDisplayName}";

                if (modCacheTime.ContainsKey(cacheKey) &&
                    (currentTime - modCacheTime[cacheKey]) < MOD_CACHE_SECONDS)
                {
                    // Return cached result if within time window
                    return modEnabledCache.ContainsKey(cacheKey) ? modEnabledCache[cacheKey] : false;
                }

                // Perform actual mod lookup
                bool isEnabled = CheckModEnabled(modDisplayName);

                // Update cache
                modEnabledCache[cacheKey] = isEnabled;
                modCacheTime[cacheKey] = currentTime;

                return isEnabled;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModStateManager] Error checking if mod is enabled: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Performs actual mod installation check without caching
        /// </summary>
        private static bool CheckModInstalled(string modDisplayName)
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
                            ModLogThrottler.LogThrottled($"Found installed mod: {modDisplayName} -> {defaultId}", "mod_found");
                            return true;
                        }

                        // Try exact match with original display name
                        if (defaultId == modDisplayName || labelId == modDisplayName)
                        {
                            ModLogThrottler.LogThrottled($"Found installed mod: {modDisplayName} -> {defaultId}", "mod_found");
                            return true;
                        }

                        // For Steam mods, try numeric ID match
                        if (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId)))
                        {
                            ModLogThrottler.LogThrottled($"Found installed Steam mod: {modDisplayName} -> {defaultId}", "mod_found");
                            return true;
                        }
                    }
                }

                // Cache missing mod and use throttled logging
                UpdateMissingModsCache();
                missingModsCache.Add(modDisplayName);
                ModLogThrottler.LogThrottled($"Mod not found: {modDisplayName} (extracted: {modId})", "missing_mods");
                return false;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModStateManager] Error in CheckModInstalled: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Performs actual mod enabled check without caching
        /// </summary>
        private static bool CheckModEnabled(string modDisplayName)
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

                return false;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModStateManager] Error in CheckModEnabled: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extracts mod ID from display name (handles various formats)
        /// </summary>
        public static string ExtractModId(string modDisplayName)
        {
            if (modDisplayName.Contains(" - "))
            {
                string[] parts = modDisplayName.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string lastPart = parts[parts.Length - 1];
                    if (Regex.IsMatch(lastPart, @"^\d+$"))
                    {
                        return lastPart;
                    }
                }
            }

            var match = Regex.Match(modDisplayName, @"\d+");
            return match.Success ? match.Value : modDisplayName;
        }

        /// <summary>
        /// Checks if a mod is a local/dev mod (not from Steam Workshop)
        /// </summary>
        public static bool IsLocalMod(string modDisplayName)
        {
            // Local mods typically don't have Steam Workshop IDs (numeric)
            string modId = ExtractModId(modDisplayName);

            // If extracted ID is the same as display name, it's likely not a Steam mod
            if (modId == modDisplayName)
            {
                return true;
            }

            // Check for common local mod patterns
            string lowerName = modDisplayName.ToLower();
            if (lowerName.Contains("local") ||
                lowerName.Contains("dev") ||
                lowerName.Contains("custom") ||
                lowerName.StartsWith("mod_") ||
                !ulong.TryParse(modId, out _)) // Not a valid Steam ID
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the current state for a mod button based on mod status
        /// </summary>
        public static ModButtonState GetModButtonState(string modDisplayName)
        {
            // Check if we have a manual state override (e.g., during installation)
            if (modStates.ContainsKey(modDisplayName))
            {
                return modStates[modDisplayName];
            }

            // Auto-determine state based on mod status
            if (IsModEnabled(modDisplayName))
            {
                return ModButtonState.Disable; // Mod is enabled - show "Disable"
            }
            else if (IsModInstalled(modDisplayName))
            {
                return ModButtonState.Enable;  // Mod installed but disabled - show "Enable"
            }
            else
            {
                return ModButtonState.Subscribe; // Mod not subscribed - show "Subscribe"
            }
        }

        /// <summary>
        /// Gets the button text for a mod based on its current state
        /// </summary>
        public static string GetModButtonText(string modDisplayName)
        {
            ModButtonState state = GetModButtonState(modDisplayName);

            switch (state)
            {
                case ModButtonState.Subscribe:
                    return "Subscribe";

                case ModButtonState.Subscribing:
                    return "Subscribing...";

                case ModButtonState.Enable:
                    return MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ENABLE;

                case ModButtonState.Disable:
                    return MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.DISABLE;

                default:
                    return "Subscribe";
            }
        }

        /// <summary>
        /// Sets a mod to subscribing state
        /// </summary>
        public static void SetModSubscribing(string modDisplayName)
        {
            modStates[modDisplayName] = ModButtonState.Subscribing;
            DebugConsole.Log($"[ModStateManager] Mod {modDisplayName} state set to Subscribing");
        }

        /// <summary>
        /// Updates a mod state after installation/operation completes
        /// </summary>
        public static void UpdateModStateAfterOperation(string modDisplayName)
        {
            // Remove manual override to auto-determine state
            if (modStates.ContainsKey(modDisplayName))
            {
                modStates.Remove(modDisplayName);
            }

            // Clear cache for this specific mod to get fresh status immediately
            string installedCacheKey = $"installed_{modDisplayName}";
            string enabledCacheKey = $"enabled_{modDisplayName}";

            if (modInstalledCache.ContainsKey(installedCacheKey))
            {
                modInstalledCache.Remove(installedCacheKey);
            }
            if (modEnabledCache.ContainsKey(enabledCacheKey))
            {
                modEnabledCache.Remove(enabledCacheKey);
            }
            if (modCacheTime.ContainsKey(installedCacheKey))
            {
                modCacheTime.Remove(installedCacheKey);
            }
            if (modCacheTime.ContainsKey(enabledCacheKey))
            {
                modCacheTime.Remove(enabledCacheKey);
            }

            DebugConsole.Log($"[ModStateManager] Mod {modDisplayName} state and cache reset for fresh verification");
        }

        /// <summary>
        /// Resets all mod states (e.g., when dialog opens)
        /// </summary>
        public static void ResetAllModStates()
        {
            modStates.Clear();
            DebugConsole.Log("[ModStateManager] All mod states reset");
        }

        /// <summary>
        /// Clears mod verification cache to ensure fresh checks
        /// </summary>
        public static void ClearModVerificationCache()
        {
            modInstalledCache.Clear();
            modEnabledCache.Clear();
            modCacheTime.Clear();
            DebugConsole.Log("[ModStateManager] Mod verification cache cleared for fresh checks");
        }

        /// <summary>
        /// Updates the missing mods cache to reduce redundant checks
        /// </summary>
        private static void UpdateMissingModsCache()
        {
            float currentTime = Time.realtimeSinceStartup;

            if ((currentTime - lastCacheUpdate) >= CACHE_REFRESH_SECONDS)
            {
                // Log consolidated missing mods report if we have any
                if (missingModsCache.Count > 0)
                {
                    DebugConsole.Log($"[ModStateManager] Summary: {missingModsCache.Count} mods not found in last {CACHE_REFRESH_SECONDS}s");
                }

                missingModsCache.Clear();
                lastCacheUpdate = currentTime;
            }
        }

        /// <summary>
        /// Checks if all required mods are enabled
        /// </summary>
        public static bool AreAllModsEnabled(string[] requiredMods)
        {
            if (requiredMods == null || requiredMods.Length == 0)
                return false;

            foreach (var mod in requiredMods)
            {
                if (!IsModEnabled(mod))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if there are disabled mods that could be enabled
        /// </summary>
        public static bool HasDisabledMods(string[] requiredMods)
        {
            if (requiredMods == null || requiredMods.Length == 0)
                return false;

            foreach (var mod in requiredMods)
            {
                if (IsModInstalled(mod) && !IsModEnabled(mod))
                {
                    return true;
                }
            }

            return false;
        }
    }
}