using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Compatibility;
using ONI_MP.DebugTools;
using Steamworks;

namespace ONI_MP.Networking.Packets.Handshake
{
    public class ModVerificationPacket : IPacket
    {
        public PacketType Type => PacketType.ModVerification;

        public CSteamID ClientSteamID;
        public string[] InstalledMods;
        public string[] ModVersions;
        public string GameVersion;
        public ulong ModsHash;

        public ModVerificationPacket()
        {
        }

        public ModVerificationPacket(CSteamID clientId)
        {
            ClientSteamID = clientId;
            CollectModInformation();
        }

        private void CollectModInformation()
        {
            try
            {
                var modManager = Global.Instance.modManager;
                var enabledMods = new List<string>();
                var modVersions = new List<string>();

                // Coletar mods ativos
                foreach (var mod in modManager.mods)
                {
                    if (mod.IsActive())
                    {
                        enabledMods.Add(mod.label.id);
                        modVersions.Add(mod.packagedModInfo?.version?.ToString() ?? "unknown");
                    }
                }

                InstalledMods = enabledMods.ToArray();
                ModVersions = modVersions.ToArray();
                GameVersion = LaunchInitializer.BuildPrefix();

                // Calcular hash dos mods para verificação rápida
                ModsHash = CalculateModsHash();

                DebugConsole.Log($"[ModVerificationPacket] Collected {InstalledMods.Length} active mods");
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[ModVerificationPacket] Error collecting mod info: {ex.Message}");
                InstalledMods = new string[0];
                ModVersions = new string[0];
                GameVersion = "unknown";
                ModsHash = 0;
            }
        }

        private ulong CalculateModsHash()
        {
            try
            {
                // Criar string combinada dos mods e versões para hash
                var combined = string.Join("|", InstalledMods) + ":" + string.Join("|", ModVersions) + ":" + GameVersion;

                // Hash simples mas eficaz
                ulong hash = 14695981039346656037UL; // FNV offset basis
                foreach (char c in combined)
                {
                    hash ^= (ulong)c;
                    hash *= 1099511628211UL; // FNV prime
                }

                return hash;
            }
            catch
            {
                return 0;
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ClientSteamID.m_SteamID);

            writer.Write(InstalledMods?.Length ?? 0);
            if (InstalledMods != null)
            {
                for (int i = 0; i < InstalledMods.Length; i++)
                {
                    writer.Write(InstalledMods[i] ?? "");
                    writer.Write(ModVersions?[i] ?? "unknown");
                }
            }

            writer.Write(GameVersion ?? "unknown");
            writer.Write(ModsHash);
        }

        public void Deserialize(BinaryReader reader)
        {
            ClientSteamID = new CSteamID(reader.ReadUInt64());

            int modCount = reader.ReadInt32();
            InstalledMods = new string[modCount];
            ModVersions = new string[modCount];

            for (int i = 0; i < modCount; i++)
            {
                InstalledMods[i] = reader.ReadString();
                ModVersions[i] = reader.ReadString();
            }

            GameVersion = reader.ReadString();
            ModsHash = reader.ReadUInt64();
        }

        public void OnDispatched()
        {
            if (!MultiplayerSession.IsHost)
            {
                DebugConsole.LogWarning("[ModVerificationPacket] Received on client - ignoring");
                return;
            }

            DebugConsole.Log($"[ModVerificationPacket] Starting verification for client {ClientSteamID}");
            DebugConsole.Log($"  Game Version: {GameVersion}");
            DebugConsole.Log($"  Mods Count: {InstalledMods?.Length ?? 0}");
            DebugConsole.Log($"  Mods Hash: {ModsHash:X16}");

            try
            {
                // Processar verificação através do ModCompatibilityManager
                DebugConsole.Log("[ModVerificationPacket] Calling ModCompatibilityManager.ValidateClientMods...");
                var result = ModCompatibilityManager.ValidateClientMods(this);

                DebugConsole.Log($"[ModVerificationPacket] Validation complete - Result: {(result.IsCompatible ? "APPROVED" : "REJECTED")}");

                // Mostrar mensagem no chat do host se cliente foi rejeitado
                if (!result.IsCompatible)
                {
                    var clientName = SteamFriends.GetFriendPersonaName(ClientSteamID);
                    ONI_MP.UI.ChatScreen.QueueMessage($"<color=red>System:</color> {clientName} was rejected due to mod incompatibility: {result.RejectReason}");
                }

                // Enviar resposta de volta
                DebugConsole.Log($"[ModVerificationPacket] Sending response to client {ClientSteamID}...");
                var response = new ModVerificationResponsePacket(ClientSteamID, result);
                bool sent = PacketSender.SendToPlayer(ClientSteamID, response);

                if (sent)
                {
                    DebugConsole.Log($"[ModVerificationPacket] Response sent successfully to {ClientSteamID}");
                }
                else
                {
                    DebugConsole.LogWarning($"[ModVerificationPacket] Failed to send response to {ClientSteamID}");
                }
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[ModVerificationPacket] Error during verification: {ex.Message}");

                // Send a rejection response even if there was an error
                try
                {
                    var errorResult = Compatibility.CompatibilityResult.CreateRejected($"Verification error: {ex.Message}");
                    var response = new ModVerificationResponsePacket(ClientSteamID, errorResult);
                    PacketSender.SendToPlayer(ClientSteamID, response);
                    DebugConsole.Log("[ModVerificationPacket] Error response sent.");
                }
                catch (System.Exception ex2)
                {
                    DebugConsole.LogWarning($"[ModVerificationPacket] Failed to send error response: {ex2.Message}");
                }
            }
        }
    }
}