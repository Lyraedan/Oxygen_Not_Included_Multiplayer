using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;

namespace ONI_MP.Networking.Relay
{
    public abstract class RelayPacketSender
    {
        public abstract bool SendToConnection(object conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle);

    }
}
