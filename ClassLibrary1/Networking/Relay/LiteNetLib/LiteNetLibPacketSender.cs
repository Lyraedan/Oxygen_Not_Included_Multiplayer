using System;
using LiteNetLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LiteNetLibPacketSender : RelayPacketSender
    {
        public override bool SendToConnection(object conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
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

        private static DeliveryMethod ConvertSendType(SteamNetworkingSend sendType)
        {
            switch (sendType)
            {
                case SteamNetworkingSend.Reliable:
                case SteamNetworkingSend.ReliableNoNagle:
                    return DeliveryMethod.ReliableOrdered;

                case SteamNetworkingSend.Unreliable:
                case SteamNetworkingSend.UnreliableNoNagle:
                case SteamNetworkingSend.UnreliableNoDelay:
                    return DeliveryMethod.Unreliable;

                default:
                    // Catch-all for any unexpected flag combos
                    if ((sendType & SteamNetworkingSend.Reliable) != 0)
                        return DeliveryMethod.ReliableOrdered;

                    return DeliveryMethod.Unreliable;
            }
        }
    }
}
