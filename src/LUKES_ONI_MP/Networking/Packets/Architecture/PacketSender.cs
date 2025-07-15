using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;
using ONI_MP.DebugTools;
using ONI_MP.Misc;

namespace ONI_MP.Networking.Packets.Architecture
{

    public static class PacketSender
    {
        public static int MAX_PACKET_SIZE_RELIABLE = 512;
        public static int MAX_PACKET_SIZE_UNRELIABLE = 1024;

        public static byte[] SerializePacket(IPacket packet)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write((byte)packet.Type);
                packet.Serialize(writer);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Send to one connection by HSteamNetConnection handle.
        /// </summary>
        public static void SendToConnection(HSteamNetConnection conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            var bytes = SerializePacket(packet);
            var _sendType = (int)sendType;

            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);

                var result = SteamNetworkingSockets.SendMessageToConnection(
                    conn, unmanagedPointer, (uint)bytes.Length, _sendType, out long msgNum);

                bool sent = result == EResult.k_EResultOK;

                if (!sent)
                {
                    DebugConsole.LogError($"[Sockets] Failed to send {packet.Type} to conn {conn} ({Utils.FormatBytes(bytes.Length)} | result: {result})", false);
                }
                else
                {
                    //DebugConsole.Log($"[Sockets] Sent {packet.Type} to conn {conn} ({Utils.FormatBytes(bytes.Length)})");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedPointer);
            }
        }

        /// <summary>
        /// Send a packet to a player by their SteamID.
        /// </summary>
        public static void SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(steamID, out var player) || player.Connection == null)
            {
                DebugConsole.LogWarning($"[PacketSender] No connection found for SteamID {steamID}");
                return;
            }

            SendToConnection(player.Connection.Value, packet, sendType);
        }

        public static void SendToHost(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            if (!MultiplayerSession.HostSteamID.IsValid())
            {
                DebugConsole.LogWarning($"[PacketSender] Failed to send to host. Host is invalid.");
                return;
            }
            SendToPlayer(MultiplayerSession.HostSteamID, packet, sendType);
        }

        /// Original single-exclude overload
        public static void SendToAll(IPacket packet, CSteamID? exclude = null, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
        {
            foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
            {
                if (exclude.HasValue && player.SteamID == exclude.Value)
                    continue;

                if (player.Connection != null)
                    SendToConnection(player.Connection.Value, packet, sendType);
            }
        }

        public static void SendToAllClients(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
        {
            if(!MultiplayerSession.IsHost)
            {
                DebugConsole.LogWarning("[PacketSender] Only the host can send to all clients");
                return;
            }
            SendToAll(packet, MultiplayerSession.HostSteamID, sendType);
        }

        public static void SendToAllExcluding(IPacket packet, HashSet<CSteamID> excludedIds, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
        {
            foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
            {
                if (excludedIds != null && excludedIds.Contains(player.SteamID))
                    continue;

                if (player.Connection != null)
                    SendToConnection(player.Connection.Value, packet, sendType);
            }
        }


    }
}
