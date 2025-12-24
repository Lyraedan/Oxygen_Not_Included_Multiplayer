using System;
using System.Collections.Generic;
using System.Linq;
using ONI_MP.Networking.Packets.Handshake;
using ONI_MP.DebugTools;
using Steamworks;

namespace ONI_MP.Networking.Compatibility
{
    public static class ModCompatibilityManager
    {
        private static List<ModInfo> _hostMods = null;
        private static Dictionary<CSteamID, ModVerificationPacket> _clientModCache = new Dictionary<CSteamID, ModVerificationPacket>();
        private static bool _strictModeEnabled = true;
        private static bool _allowVersionMismatches = false;

        // Ignore version checking for now - focus only on mod presence
        private const bool IGNORE_VERSION_CHECKS = true;

        public static void Initialize()
        {
            DebugConsole.Log("[ModCompatibilityManager] Initializing...");

            if (MultiplayerSession.IsHost)
            {
                CollectHostMods();
            }

            // Clear client cache
            _clientModCache.Clear();
        }

        public static void Shutdown()
        {
            DebugConsole.Log("[ModCompatibilityManager] Shutting down...");
            _hostMods = null;
            _clientModCache.Clear();
        }

        public static void SetStrictMode(bool enabled)
        {
            _strictModeEnabled = enabled;
            DebugConsole.Log($"[ModCompatibilityManager] Strict mode: {(enabled ? "ENABLED" : "DISABLED")}");
        }

        public static void SetAllowVersionMismatches(bool allowed)
        {
            _allowVersionMismatches = allowed;
            DebugConsole.Log($"[ModCompatibilityManager] Version mismatches: {(allowed ? "ALLOWED" : "BLOCKED")}");
        }

        private static void CollectHostMods()
        {
            try
            {
                _hostMods = new List<ModInfo>();
                var modManager = Global.Instance.modManager;

                foreach (var mod in modManager.mods)
                {
                    if (mod.IsActive())
                    {
                        var modInfo = new ModInfo(
                            mod.label.id,
                            mod.packagedModInfo?.version?.ToString() ?? "unknown",
                            mod.title,
                            true // By default, all mods are required
                        );

                        // Configure if version mismatch is allowed
                        modInfo.AllowVersionMismatch = _allowVersionMismatches;

                        _hostMods.Add(modInfo);
                    }
                }

                DebugConsole.Log($"[ModCompatibilityManager] Host has {_hostMods.Count} active mods");
                foreach (var mod in _hostMods)
                {
                    DebugConsole.Log($"  - {mod}");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityManager] Error collecting host mods: {ex.Message}");
                _hostMods = new List<ModInfo>();
            }
        }

        public static CompatibilityResult ValidateClientMods(ModVerificationPacket clientPacket)
        {
            if (!MultiplayerSession.IsHost)
            {
                DebugConsole.LogWarning("[ModCompatibilityManager] ValidateClientMods called on non-host!");
                return CompatibilityResult.CreateRejected("Internal error: not a host");
            }

            if (_hostMods == null)
            {
                CollectHostMods();
            }

            // Cache client packet
            _clientModCache[clientPacket.ClientSteamID] = clientPacket;

            var result = new CompatibilityResult();

            try
            {
                // Check game version
                var hostGameVersion = LaunchInitializer.BuildPrefix();
                if (clientPacket.GameVersion != hostGameVersion)
                {
                    result.RejectReason = $"Game version mismatch: Host={hostGameVersion}, Client={clientPacket.GameVersion}";
                    DebugConsole.Log($"[ModCompatibilityManager] {result.RejectReason}");
                    return result;
                }

                // Converter mods do cliente para ModInfo
                var clientMods = new List<ModInfo>();
                for (int i = 0; i < clientPacket.InstalledMods.Length; i++)
                {
                    var modInfo = new ModInfo(
                        clientPacket.InstalledMods[i],
                        i < clientPacket.ModVersions.Length ? clientPacket.ModVersions[i] : "unknown"
                    );
                    clientMods.Add(modInfo);
                }

                DebugConsole.Log($"[ModCompatibilityManager] Validating client {clientPacket.ClientSteamID} with {clientMods.Count} mods");
                DebugConsole.Log($"[ModCompatibilityManager] Host has {_hostMods.Count} mods");

                // Log detailed mod lists for debugging
                DebugConsole.Log("[ModCompatibilityManager] Host mods:");
                foreach (var hostMod in _hostMods)
                {
                    DebugConsole.Log($"  Host: {hostMod.StaticID} - {hostMod.Version}");
                }

                DebugConsole.Log("[ModCompatibilityManager] Client mods:");
                foreach (var clientMod in clientMods)
                {
                    DebugConsole.Log($"  Client: {clientMod.StaticID} - {clientMod.Version}");
                }

                // Check required mods that are missing on client
                foreach (var hostMod in _hostMods)
                {
                    var clientMod = clientMods.FirstOrDefault(c => c.StaticID == hostMod.StaticID);

                    if (clientMod == null)
                    {
                        // Always require host mods to be present on client
                        result.AddMissingMod(hostMod.StaticID, GetModName(hostMod.StaticID));
                        DebugConsole.Log($"  Missing required mod: {hostMod}");
                    }
                    else if (hostMod.HasVersionMismatch(clientMod))
                    {
                        if (!IGNORE_VERSION_CHECKS && !hostMod.AllowVersionMismatch && !_allowVersionMismatches)
                        {
                            result.AddVersionMismatch(hostMod.StaticID, GetModName(hostMod.StaticID));
                            DebugConsole.Log($"  Version mismatch: {hostMod} vs {clientMod}");
                        }
                        else
                        {
                            result.AddWarning($"Version mismatch (ignored): {hostMod} vs {clientMod}");
                            DebugConsole.Log($"  Version mismatch ignored: {hostMod} vs {clientMod}");
                        }
                    }
                }

                // Log extra mods but DO NOT reject for them (permissive policy)
                foreach (var clientMod in clientMods)
                {
                    var hostMod = _hostMods.FirstOrDefault(h => h.StaticID == clientMod.StaticID);

                    if (hostMod == null)
                    {
                        // Client has extra mod that host doesn't have - log but allow
                        result.AddExtraMod(clientMod.StaticID, GetModName(clientMod.StaticID));
                        DebugConsole.Log($"  Client has extra mod (allowed): {clientMod}");
                    }
                }

                // Check overall mod hash for quick validation
                if (result.IsCompatible && clientMods.Count == _hostMods.Count)
                {
                    // If mod count is equal, we can use hash for quick verification
                    var hostHash = CalculateHostModsHash();
                    if (clientPacket.ModsHash != hostHash)
                    {
                        DebugConsole.Log($"[ModCompatibilityManager] Hash mismatch: Host={hostHash:X16}, Client={clientPacket.ModsHash:X16}");
                        // Don't reject by hash, as it could be mod order difference
                        result.AddWarning("Mod hash mismatch - possible mod order difference");
                    }
                }

                // Determine final result - only reject for missing mods or version mismatches
                // Extra mods are allowed (permissive policy)
                if (result.MissingMods.Count == 0 && result.VersionMismatches.Count == 0)
                {
                    result.IsCompatible = true;
                    if (result.ExtraMods.Count > 0)
                    {
                        result.RejectReason = $"Compatible (client has {result.ExtraMods.Count} extra mod(s))";
                        DebugConsole.Log($"[ModCompatibilityManager] Client {clientPacket.ClientSteamID} APPROVED with extra mods");
                    }
                    else
                    {
                        result.RejectReason = "Compatible";
                        DebugConsole.Log($"[ModCompatibilityManager] Client {clientPacket.ClientSteamID} APPROVED");
                    }
                }
                else
                {
                    result.IsCompatible = false;
                    var issues = new List<string>();

                    if (result.MissingMods.Count > 0)
                        issues.Add($"{result.MissingMods.Count} missing mods");
                    if (result.ExtraMods.Count > 0)
                        issues.Add($"{result.ExtraMods.Count} extra mods");
                    if (result.VersionMismatches.Count > 0)
                        issues.Add($"{result.VersionMismatches.Count} version mismatches");

                    result.RejectReason = $"Mod incompatibility: {string.Join(", ", issues)}";

                    DebugConsole.Log($"[ModCompatibilityManager] Client {clientPacket.ClientSteamID} REJECTED: {result.RejectReason}");
                }

                return result;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityManager] Error validating client mods: {ex.Message}");
                result.RejectReason = $"Validation error: {ex.Message}";
                return result;
            }
        }

        public static List<ModInfo> GetHostMods()
        {
            return _hostMods?.ToList() ?? new List<ModInfo>();
        }

        public static ModVerificationPacket GetClientMods(CSteamID clientId)
        {
            return _clientModCache.TryGetValue(clientId, out var packet) ? packet : null;
        }

        public static void RemoveClientFromCache(CSteamID clientId)
        {
            _clientModCache.Remove(clientId);
        }

        private static ulong CalculateHostModsHash()
        {
            if (_hostMods == null || _hostMods.Count == 0)
                return 0;

            try
            {
                // Ordenar mods por ID para garantir hash consistente
                var sortedMods = _hostMods.OrderBy(m => m.StaticID).ToList();
                var combined = string.Join("|", sortedMods.Select(m => $"{m.StaticID}:{m.Version}"));
                combined += ":" + LaunchInitializer.BuildPrefix();

                // Hash FNV-1a
                ulong hash = 14695981039346656037UL;
                foreach (char c in combined)
                {
                    hash ^= (ulong)c;
                    hash *= 1099511628211UL;
                }

                return hash;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetModName(string modId)
        {
            try
            {
                // Try to find mod by ID in the active mods
                var activeMods = Global.Instance?.modManager?.mods;
                if (activeMods != null)
                {
                    foreach (var mod in activeMods)
                    {
                        if (mod?.label != null && mod.label.id == modId)
                        {
                            // Return "ModName - ID" format for better UX
                            string displayName = !string.IsNullOrEmpty(mod.title) ? mod.title : mod.label.title;
                            return $"{displayName} - {modId}";
                        }
                    }
                }

                // Fallback: try to get from mod manager instance
                var modManager = Global.Instance?.modManager;
                if (modManager != null)
                {
                    var allMods = modManager.mods;
                    if (allMods != null)
                    {
                        foreach (var mod in allMods)
                        {
                            if (mod?.label != null && mod.label.id == modId)
                            {
                                string displayName = !string.IsNullOrEmpty(mod.title) ? mod.title : mod.label.title;
                                return $"{displayName} - {modId}";
                            }
                        }
                    }
                }

                // If we can't find the mod name, return just the ID
                return modId;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityManager] Failed to get mod name for {modId}: {ex.Message}");
                return modId;
            }
        }

        public static string GetCompatibilityReport()
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== MOD COMPATIBILITY REPORT ===");
            report.AppendLine($"Host Mods Count: {_hostMods?.Count ?? 0}");
            report.AppendLine($"Cached Clients: {_clientModCache.Count}");
            report.AppendLine($"Strict Mode: {_strictModeEnabled}");
            report.AppendLine($"Allow Version Mismatches: {_allowVersionMismatches}");
            report.AppendLine();

            if (_hostMods != null && _hostMods.Count > 0)
            {
                report.AppendLine("Host Mods:");
                foreach (var mod in _hostMods)
                {
                    report.AppendLine($"  - {mod}");
                }
                report.AppendLine();
            }

            if (_clientModCache.Count > 0)
            {
                report.AppendLine("Client Mods:");
                foreach (var kvp in _clientModCache)
                {
                    report.AppendLine($"  Client {kvp.Key}:");
                    for (int i = 0; i < kvp.Value.InstalledMods.Length; i++)
                    {
                        var version = i < kvp.Value.ModVersions.Length ? kvp.Value.ModVersions[i] : "unknown";
                        report.AppendLine($"    - {kvp.Value.InstalledMods[i]} v{version}");
                    }
                }
            }

            return report.ToString();
        }
    }
}