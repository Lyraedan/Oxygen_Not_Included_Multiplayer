using System;
using Riptide;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Transport.Lan
{
    public class RiptidePacketSender : TransportPacketSender
    {
        public override bool SendToConnection(object conn,IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            if (conn is not Connection)
                return false;

            Connection connection = conn as Connection;
            if (!connection.IsConnected)
                return false;

            byte[] bytes = PacketSender.SerializePacketForSending(packet);
            MessageSendMode sendMode = ConvertSendType(sendType);

            Riptide.Message msg = Riptide.Message.Create(sendMode, 1); // dummy ID
            msg.AddBytes(bytes);

            if(MultiplayerSession.IsHost)
            {
                RiptideServer.Client.Send(msg);
            } else
            {
                RiptideClient.Client.Send(msg);
            }

            return true;
        }

        private static MessageSendMode ConvertSendType(SteamNetworkingSend sendType)
        {
            switch (sendType)
            {
                case SteamNetworkingSend.Reliable:
                case SteamNetworkingSend.ReliableNoNagle:
                    return MessageSendMode.Reliable;

                case SteamNetworkingSend.Unreliable:
                case SteamNetworkingSend.UnreliableNoNagle:
                case SteamNetworkingSend.UnreliableNoDelay:
                    return MessageSendMode.Unreliable;

                default:
                    // Catch-all for unexpected flag combinations
                    if ((sendType & SteamNetworkingSend.Reliable) != 0)
                        return MessageSendMode.Reliable;

                    return MessageSendMode.Unreliable;
            }
        }
    }
}
