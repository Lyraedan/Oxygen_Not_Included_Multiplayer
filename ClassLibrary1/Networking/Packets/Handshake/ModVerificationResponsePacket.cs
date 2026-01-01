using System;
using System.IO;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Compatibility;
using ONI_MP.DebugTools;
using Steamworks;

namespace ONI_MP.Networking.Packets.Handshake
{
    public class ModVerificationResponsePacket : IPacket
    {
        public CSteamID ClientSteamID;
        public bool IsApproved;
        public string RejectReason;
        public string[] MissingMods;
        public string[] ExtraMods;
        public string[] VersionMismatches;
        public ulong[] SteamModIds;  // Steam Workshop IDs for auto-installation

        public ModVerificationResponsePacket()
        {
        }

        public ModVerificationResponsePacket(CSteamID clientId, CompatibilityResult result)
        {
            ClientSteamID = clientId;
            IsApproved = result.IsCompatible;
            RejectReason = result.RejectReason;
            MissingMods = result.MissingMods?.ToArray() ?? new string[0];
            ExtraMods = result.ExtraMods?.ToArray() ?? new string[0];
            VersionMismatches = result.VersionMismatches?.ToArray() ?? new string[0];

            // Extract Steam Workshop IDs from missing mods
            SteamModIds = ExtractSteamModIds(MissingMods);
        }

        private static ulong[] ExtractSteamModIds(string[] modIds)
        {
            if (modIds == null || modIds.Length == 0)
                return new ulong[0];

            var steamIds = new System.Collections.Generic.List<ulong>();
            foreach (var modId in modIds)
            {
                // Steam mods have numeric IDs, try to parse them
                if (ulong.TryParse(modId, out var steamId))
                {
                    steamIds.Add(steamId);
                }
            }
            return steamIds.ToArray();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ClientSteamID.m_SteamID);
            writer.Write(IsApproved);
            writer.Write(RejectReason ?? "");

            writer.Write(MissingMods?.Length ?? 0);
            if (MissingMods != null)
            {
                foreach (var mod in MissingMods)
                {
                    writer.Write(mod ?? "");
                }
            }

            writer.Write(ExtraMods?.Length ?? 0);
            if (ExtraMods != null)
            {
                foreach (var mod in ExtraMods)
                {
                    writer.Write(mod ?? "");
                }
            }

            writer.Write(VersionMismatches?.Length ?? 0);
            if (VersionMismatches != null)
            {
                foreach (var mod in VersionMismatches)
                {
                    writer.Write(mod ?? "");
                }
            }

            writer.Write(SteamModIds?.Length ?? 0);
            if (SteamModIds != null)
            {
                foreach (var steamId in SteamModIds)
                {
                    writer.Write(steamId);
                }
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            ClientSteamID = new CSteamID(reader.ReadUInt64());
            IsApproved = reader.ReadBoolean();
            RejectReason = reader.ReadString();

            int missingCount = reader.ReadInt32();
            MissingMods = new string[missingCount];
            for (int i = 0; i < missingCount; i++)
            {
                MissingMods[i] = reader.ReadString();
            }

            int extraCount = reader.ReadInt32();
            ExtraMods = new string[extraCount];
            for (int i = 0; i < extraCount; i++)
            {
                ExtraMods[i] = reader.ReadString();
            }

            int versionCount = reader.ReadInt32();
            VersionMismatches = new string[versionCount];
            for (int i = 0; i < versionCount; i++)
            {
                VersionMismatches[i] = reader.ReadString();
            }

            int steamIdCount = reader.ReadInt32();
            SteamModIds = new ulong[steamIdCount];
            for (int i = 0; i < steamIdCount; i++)
            {
                SteamModIds[i] = reader.ReadUInt64();
            }
        }

        public void OnDispatched()
        {
            DebugConsole.Log($"[ModVerificationResponsePacket] Received packet - IsHost: {MultiplayerSession.IsHost}, ClientSteamID: {ClientSteamID}");

            if (MultiplayerSession.IsHost)
            {
                DebugConsole.LogWarning("[ModVerificationResponsePacket] Received on host - ignoring");
                return;
            }

            // Double check: if this packet is for the host, ignore it
            if (ClientSteamID == MultiplayerSession.HostSteamID)
            {
                DebugConsole.LogWarning("[ModVerificationResponsePacket] Response packet addressed to host - ignoring");
                return;
            }

            // Verify this packet is for our client
            if (ClientSteamID != MultiplayerSession.LocalSteamID)
            {
                DebugConsole.LogWarning($"[ModVerificationResponsePacket] Response for different client {ClientSteamID}, local is {MultiplayerSession.LocalSteamID} - ignoring");
                return;
            }

            DebugConsole.Log($"[ModVerificationResponsePacket] Processing response: {(IsApproved ? "APPROVED" : "REJECTED")}");

            if (IsApproved)
            {
                DebugConsole.Log("[ModVerificationResponsePacket] Mod verification successful - connection approved");

                // Cliente foi aprovado, pode prosseguir com o handshake normal
                GameClient.OnModVerificationApproved();
            }
            else
            {
                DebugConsole.Log($"[ModVerificationResponsePacket] Mod verification failed: {RejectReason}");

                if (MissingMods.Length > 0)
                {
                    DebugConsole.Log($"  Missing mods: {string.Join(", ", MissingMods)}");
                }

                if (ExtraMods.Length > 0)
                {
                    DebugConsole.Log($"  Extra mods: {string.Join(", ", ExtraMods)}");
                }

                if (VersionMismatches.Length > 0)
                {
                    DebugConsole.Log($"  Version mismatches: {string.Join(", ", VersionMismatches)}");
                }

                // Mostrar erro para o usuário e desconectar
                GameClient.OnModVerificationRejected(RejectReason, MissingMods, ExtraMods, VersionMismatches, SteamModIds);
            }
        }
    }
}