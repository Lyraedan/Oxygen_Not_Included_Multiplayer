using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ONI_MP.Networking
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
                int packet_type = PacketRegistry.GetPacketId(packet);
                writer.Write(packet_type);
                packet.Serialize(writer);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Send to one connection by HSteamNetConnection handle.
        /// </summary>
        public static bool SendToConnection(HSteamNetConnection conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            // 如果是直连模式，使用直连发送
            if (DirectConnection.Mode == ConnectionMode.DirectIP)
            {
                return SendViaDirectConnection(packet, null);
            }

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
                    // DebugConsole.LogError($"[Sockets] Failed to send {packet.Type} to conn {conn} ({Utils.FormatBytes(bytes.Length)} | result: {result})", false);
                }
                else
                {
                    PacketTracker.TrackSent(new PacketTracker.PacketTrackData {
                        packet = packet,
                        size = bytes.Length
                    });
                }
                return sent;
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedPointer);
            }
        }

        /// <summary>
        /// Send a packet to a player by their SteamID.
        /// </summary>
        public static bool SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            // 如果是直连模式
            if (DirectConnection.Mode == ConnectionMode.DirectIP)
            {
                return SendViaDirectConnection(packet, steamID.ToString());
            }

            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(steamID, out var player) || player.Connection == null)
            {
                DebugConsole.LogWarning($"[PacketSender] No connection found for SteamID {steamID}");
                return false;
            }

            return SendToConnection(player.Connection.Value, packet, sendType);
        }

        public static void SendToHost(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            // 如果是直连模式，客户端直接发给服务器
            if (DirectConnection.Mode == ConnectionMode.DirectIP)
            {
                SendViaDirectConnection(packet, null, isToHost: true);
                return;
            }

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
            // 如果是直连模式，广播给所有客户端
            if (DirectConnection.Mode == ConnectionMode.DirectIP)
            {
                BroadcastViaDirectConnection(packet);
                return;
            }

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
            if (!MultiplayerSession.IsHost)
            {
                DebugConsole.LogWarning("[PacketSender] Only the host can send to all clients");
                return;
            }

            // 如果是直连模式
            if (DirectConnection.Mode == ConnectionMode.DirectIP)
            {
                BroadcastViaDirectConnection(packet);
                return;
            }

            SendToAll(packet, MultiplayerSession.HostSteamID, sendType);
        }

        public static void SendToAllExcluding(IPacket packet, HashSet<CSteamID> excludedIds, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
        {
            // 直连模式暂时不支持排除，直接广播
            if (DirectConnection.Mode == ConnectionMode.DirectIP)
            {
                BroadcastViaDirectConnection(packet);
                return;
            }

            foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
            {
                if (excludedIds != null && excludedIds.Contains(player.SteamID))
                    continue;

                if (player.Connection != null)
                    SendToConnection(player.Connection.Value, packet, sendType);
            }
        }

        /// <summary>
        /// Sends a packet to all other players.
        /// if sent from the host, it goes to all clients.
        /// otherwise it is wrapped in a HostBroadcastPacket and sent to the host for rebroadcasting.
        /// </summary>
        public static void SendToAllOtherPeers(IPacket packet)
        {
            if (!MultiplayerSession.InSession)
            {
                DebugConsole.LogWarning("[PacketSender] Not in a multiplayer session, cannot send to other peers");
                return;
            }
            DebugConsole.Log("[PacketSender] Sending packet to all other peers: " + packet.GetType().Name);

            if (MultiplayerSession.IsHost)
                SendToAllClients(packet);
            else
                SendToHost(new HostBroadcastPacket(packet, MultiplayerSession.LocalSteamID));
        }

        #region Direct Connection Methods

        /// <summary>
        /// 通过直连发送数据包
        /// </summary>
        /// <param name="packet">要发送的数据包</param>
        /// <param name="targetClientId">目标客户端ID（主机用），null表示发给服务器（客户端用）</param>
        /// <param name="isToHost">是否发给主机</param>
        private static bool SendViaDirectConnection(IPacket packet, string targetClientId, bool isToHost = false)
        {
            try
            {
                var bytes = SerializePacket(packet);

                if (isToHost || !MultiplayerSession.IsHost)
                {
                    // 客户端发给主机
                    DirectConnection.SendToServer(bytes);
                }
                else if (targetClientId != null)
                {
                    // 主机发给特定客户端
                    DirectConnection.SendToClient(targetClientId, bytes);
                }

                PacketTracker.TrackSent(new PacketTracker.PacketTrackData
                {
                    packet = packet,
                    size = bytes.Length
                });

                return true;
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[PacketSender] Direct connection send failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 通过直连广播给所有客户端（主机用）
        /// </summary>
        private static void BroadcastViaDirectConnection(IPacket packet)
        {
            try
            {
                var bytes = SerializePacket(packet);
                DirectConnection.Broadcast(bytes);

                PacketTracker.TrackSent(new PacketTracker.PacketTrackData
                {
                    packet = packet,
                    size = bytes.Length
                });
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[PacketSender] Direct connection broadcast failed: {ex.Message}");
            }
        }

        #endregion

        #region API Methods (保持不变)

        public static void SendToAllOtherPeers_API(object api_packet)
        {
            var type = api_packet.GetType();
            if (!PacketRegistry.HasRegisteredPacket(type))
            {
                DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
                return;
            }
            if (!API_Helper.WrapApiPacket(api_packet, out var packet))
            {
                DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
                return;
            }
            SendToAllOtherPeers(packet);
        }

        public static void SendToAll_API(object api_packet, CSteamID? exclude = null, int sendType = (int)SteamNetworkingSend.Reliable)
        {
            var type = api_packet.GetType();
            if (!PacketRegistry.HasRegisteredPacket(type))
            {
                DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
                return;
            }
            if (!API_Helper.WrapApiPacket(api_packet, out var packet))
            {
                DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
                return;
            }
            SendToAll(packet, exclude, (SteamNetworkingSend)sendType);
        }

        public static void SendToAllClients_API(object api_packet, int sendType = (int)SteamNetworkingSend.Reliable)
        {
            var type = api_packet.GetType();
            if (!PacketRegistry.HasRegisteredPacket(type))
            {
                DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
                return;
            }

            if (!API_Helper.WrapApiPacket(api_packet, out var packet))
            {
                DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
                return;
            }
            SendToAllClients(packet, (SteamNetworkingSend)sendType);
        }

        public static void SendToAllExcluding_API(object api_packet, HashSet<CSteamID> excludedIds, int sendType = (int)SteamNetworkingSend.Reliable)
        {
            var type = api_packet.GetType();
            if (!PacketRegistry.HasRegisteredPacket(type))
            {
                DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
                return;
            }

            if (!API_Helper.WrapApiPacket(api_packet, out var packet))
            {
                DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
                return;
            }
            SendToAllExcluding(packet, excludedIds, (SteamNetworkingSend)sendType);
        }

        public static void SendToPlayer_API(CSteamID steamID, object api_packet, int sendType = (int)SteamNetworkingSend.ReliableNoNagle)
        {
            var type = api_packet.GetType();
            if (!PacketRegistry.HasRegisteredPacket(type))
            {
                DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
                return;
            }

            if (!API_Helper.WrapApiPacket(api_packet, out var packet))
            {
                DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
                return;
            }
            SendToPlayer(steamID, packet, (SteamNetworkingSend)sendType);
        }

        public static void SendToHost_API(object api_packet, int sendType = (int)SteamNetworkingSend.ReliableNoNagle)
        {
            var type = api_packet.GetType();
            if (!PacketRegistry.HasRegisteredPacket(type))
            {
                DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
                return;
            }

            if (!API_Helper.WrapApiPacket(api_packet, out var packet))
            {
                DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
                return;
            }
            SendToHost(packet, (SteamNetworkingSend)sendType);
        }

        #endregion
    }
}
