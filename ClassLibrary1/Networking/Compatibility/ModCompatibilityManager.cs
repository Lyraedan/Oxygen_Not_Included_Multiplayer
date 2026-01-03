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

        // Configuration loaded from host settings
        private static bool _modVerificationEnabled = true;
        private static bool _allowVersionMismatches = false;
        private static bool _allowExtraMods = true;

        public static void Initialize()
        {
            DebugConsole.Log("[ModCompatibilityManager] Initializing...");

            // Load configuration from host settings
            LoadConfiguration();

            if (MultiplayerSession.IsHost)
            {
                CollectHostMods();
            }

            // Clear client cache
            _clientModCache.Clear();
        }

        private static void LoadConfiguration()
        {
            try
            {
                var config = Configuration.Instance.Host;

                _modVerificationEnabled = config.EnableModCompatibilityCheck;
                _allowVersionMismatches = config.AllowVersionMismatches;
                _allowExtraMods = config.AllowExtraMods;

                DebugConsole.Log($"[ModCompatibilityManager] Configuration loaded:");
                DebugConsole.Log($"  - Mod verification: {(_modVerificationEnabled ? "ENABLED" : "DISABLED")}");
                DebugConsole.Log($"  - Allow version mismatches: {(_allowVersionMismatches ? "YES" : "NO")}");
                DebugConsole.Log($"  - Allow extra mods: {(_allowExtraMods ? "YES" : "NO")}");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityManager] Error loading configuration: {ex.Message}");
                DebugConsole.Log("[ModCompatibilityManager] Using default settings");
            }
        }

        public static bool IsModVerificationEnabled()
        {
            return _modVerificationEnabled;
        }

        public static void Shutdown()
        {
            DebugConsole.Log("[ModCompatibilityManager] Shutting down...");
            _hostMods = null;
            _clientModCache.Clear();
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
                    // Use IsEnabledForActiveDlc() instead of IsActive() - follows ONI standard
                    if (mod.IsEnabledForActiveDlc())
                    {
                        var modInfo = new ModInfo(
                            mod.label.defaultStaticID, // Use defaultStaticID for consistent identification
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
                        // Always require host mods to be present on client - use friendly name for better UX
                        result.AddMissingMod(GetModName(hostMod.StaticID));
                        DebugConsole.Log($"  Missing required mod: {hostMod.StaticID}");
                    }
                    else if (hostMod.HasVersionMismatch(clientMod))
                    {
                        if (!_allowVersionMismatches && !hostMod.AllowVersionMismatch)
                        {
                            result.AddVersionMismatch(GetModName(hostMod.StaticID));
                            DebugConsole.Log($"  Version mismatch: {hostMod.StaticID} vs {clientMod.StaticID}");
                        }
                        else
                        {
                            result.AddWarning($"Version mismatch (ignored): {GetModName(hostMod.StaticID)} vs {GetModName(clientMod.StaticID)}");
                            DebugConsole.Log($"  Version mismatch ignored: {hostMod.StaticID} vs {clientMod.StaticID}");
                        }
                    }
                }

                // Handle extra mods based on configuration
                foreach (var clientMod in clientMods)
                {
                    var hostMod = _hostMods.FirstOrDefault(h => h.StaticID == clientMod.StaticID);

                    if (hostMod == null)
                    {
                        // Client has extra mod that host doesn't have
                        result.AddExtraMod(GetModName(clientMod.StaticID));

                        if (_allowExtraMods)
                        {
                            DebugConsole.Log($"  Client has extra mod (allowed): {clientMod.StaticID}");
                        }
                        else
                        {
                            DebugConsole.Log($"  Client has extra mod (rejected): {clientMod.StaticID}");
                        }
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

                // Determine final result based on configuration
                var hasMissingMods = result.MissingMods.Count > 0;
                var hasVersionMismatches = result.VersionMismatches.Count > 0;
                var hasExtraMods = result.ExtraMods.Count > 0;

                // Check if we should reject based on extra mods
                var shouldRejectExtraMods = hasExtraMods && !_allowExtraMods;

                if (!hasMissingMods && !hasVersionMismatches && !shouldRejectExtraMods)
                {
                    result.IsCompatible = true;
                    if (hasExtraMods && _allowExtraMods)
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

                    if (hasMissingMods)
                        issues.Add($"{result.MissingMods.Count} missing mods");
                    if (shouldRejectExtraMods)
                        issues.Add($"{result.ExtraMods.Count} extra mods not allowed");
                    if (hasVersionMismatches)
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
                        // Use defaultStaticID for consistent mod identification
                        if (mod?.label != null && mod.label.defaultStaticID == modId)
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
                            // Use defaultStaticID for consistent mod identification
                            if (mod?.label != null && mod.label.defaultStaticID == modId)
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
            report.AppendLine($"Allow Version Mismatches: {_allowVersionMismatches}");
            report.AppendLine($"Allow Extra Mods: {_allowExtraMods}");
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