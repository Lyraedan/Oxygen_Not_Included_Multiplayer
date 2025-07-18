using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Relay;

namespace ONI_MP.Networking.Packets.Architecture
{
    public static class PacketSender
    {
        public static INetworkPlatform Platform { get; set; } = null;

        public static int MAX_PACKET_SIZE_RELIABLE = 512;
        public static int MAX_PACKET_SIZE_UNRELIABLE = 1024;

        public static byte[] SerializePacket(IPacket packet)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                using (var writer = new System.IO.BinaryWriter(ms))
                {
                    writer.Write((byte)packet.Type);
                    packet.Serialize(writer);
                }
                return ms.ToArray();
            }
        }

        public static void SendToConnection(INetworkConnection conn, IPacket packet, SendType sendType = SendType.Reliable)
        {
            if (!conn.IsValid)
            {
                DebugConsole.LogWarning($"[PacketSender] Attempted to send to invalid connection: {conn.DebugName}");
                return;
            }

            DebugConsole.Log($"[PacketSender] Sending {packet.Type} to {conn.DebugName}");
            Misc.Scheduler.Instance.Once(() => conn.Send(packet, sendType), Misc.Scheduler.Pipeline.NETWORK);
        }

        public static void SendToPlayer(string id, IPacket packet, SendType sendType = SendType.Reliable)
        {
            var target = FindConnectionById(id);

            if (target == null || !target.IsValid)
            {
                DebugConsole.LogWarning($"[PacketSender] No valid connection found for Id: {id}");
                return;
            }

            SendToConnection(target, packet, sendType );
        }


        public static void SendToHost(IPacket packet, SendType sendType = SendType.Reliable)
        {
            if (Platform.HostConnection == null || !Platform.HostConnection.IsValid)
            {
                DebugConsole.LogWarning("[PacketSender] Host connection is invalid.");
                return;
            }

            SendToConnection(Platform.HostConnection, packet, sendType );
        }

        public static void SendToAll(IPacket packet, INetworkConnection exclude = null, SendType sendType = SendType.Reliable)
        {
            Misc.Scheduler.Instance.Once(() => Platform.SendToAll(packet, exclude, sendType), Misc.Scheduler.Pipeline.NETWORK);
        }

        public static void SendToAllClients(IPacket packet, SendType sendType = SendType.Reliable)
        {
            if (!Platform.IsHost)
            {
                DebugConsole.LogWarning("[PacketSender] Only the host can send to all clients.");
                return;
            }

            SendToAll(packet, Platform.HostConnection, sendType);
        }

        public static void SendToAllExcluding(IPacket packet, HashSet<string> excludedIds, SendType sendType = SendType.Reliable)
        {
            var excludeSet = excludedIds ?? new HashSet<string>();

            var filteredConnections = new HashSet<INetworkConnection>();
            foreach (var conn in Platform.ConnectedClients)
            {
                if (excludeSet.Contains(conn.Id))
                    filteredConnections.Add(conn);
            }

            Misc.Scheduler.Instance.Once(() => Platform.SendToAllExcluding(packet, filteredConnections, sendType), Misc.Scheduler.Pipeline.NETWORK);
        }

        private static INetworkConnection FindConnectionById(string id)
        {
            foreach (var conn in Platform.ConnectedClients)
            {
                if (conn.Id == id)
                    return conn;
            }
            return null;
        }

    }
}
