using System;
using LiteNetLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Profiling;

namespace ONI_MP.Networking.Transport.Lan
{
    public class LiteNetLibPacketSender : TransportPacketSender
    {
        public override bool SendToConnection(object conn, IPacket packet, PacketSendMode sendType = PacketSendMode.ReliableImmediate)
        {
            using var _ = Profiler.Scope();

            DebugConsole.Log(string.Format("[LanPacketSender] Connection is NetPeer: {0}", conn is not NetPeer));

            if (conn is not NetPeer)
                return false;

            NetPeer peer = (NetPeer)conn;
            DebugConsole.Log(string.Format("[LanPacketSender] Connection state: {0}", peer.ConnectionState));
            if (peer.ConnectionState != ConnectionState.Connected)
                return false;

            byte[] bytes = PacketSender.SerializePacketForSending(packet);
            DeliveryMethod deliveryMethod = ConvertSendType(sendType);

            DebugConsole.Log($"[LanPacketSender] Sending {bytes.Length} bytes ({deliveryMethod})");

            try
            {
                peer.Send(bytes, deliveryMethod);
                return true;
            }
            catch (Exception e)
            {
                DebugConsole.LogError($"[LanPacketSender] Send failed: {e}", false);
                return false;
            }
        }

        private static DeliveryMethod ConvertSendType(PacketSendMode sendType)
        {
            using var _ = Profiler.Scope();

            switch (sendType)
            {
                case PacketSendMode.Reliable:
                case PacketSendMode.ReliableImmediate:
                    return DeliveryMethod.ReliableOrdered;

                case PacketSendMode.Unreliable:
                case PacketSendMode.UnreliableImmediate:
                case PacketSendMode.UnreliableNoDelay:
                    return DeliveryMethod.Unreliable;

                default:
                    // Catch-all for any unexpected flag combos
                    if ((sendType & PacketSendMode.Reliable) != 0)
                        return DeliveryMethod.ReliableOrdered;

                    return DeliveryMethod.Unreliable;
            }
        }
    }
}
