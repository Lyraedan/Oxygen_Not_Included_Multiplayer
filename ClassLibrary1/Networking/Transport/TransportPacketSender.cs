using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Transport
{
    public abstract class TransportPacketSender
    {
        public abstract bool SendToConnection(object conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle);

    }
}
