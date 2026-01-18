using System;
using Riptide;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Relay.Lan
{
    public class RiptidePacketSender : RelayPacketSender
    {
        public override bool SendToConnection(
            object conn,
            IPacket packet,
            SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            DebugConsole.Log($"[LanPacketSender] Connection is Riptide Connection: {conn is Connection}");

            if (conn is not Connection connection)
                return false;

            if (!connection.IsConnected)
                return false;

            byte[] bytes = PacketSender.SerializePacketForSending(packet);
            MessageSendMode sendMode = ConvertSendType(sendType);

            DebugConsole.Log($"[LanPacketSender] Sending {bytes.Length} bytes ({sendMode})");

            try
            {
                Riptide.Message msg = Riptide.Message.Create(sendMode);
                msg.AddBytes(bytes);

                connection.Send(msg);
                return true;
            }
            catch (Exception e)
            {
                DebugConsole.LogError($"[LanPacketSender] Send failed: {e}", false);
                return false;
            }
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
