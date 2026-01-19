using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;

namespace ONI_MP.Networking.Transport
{
    public abstract class TransportPacketSender
    {
        public abstract bool SendToConnection(object conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle);

    }
}
