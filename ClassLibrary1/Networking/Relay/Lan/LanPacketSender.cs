using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LanPacketSender : RelayPacketSender
    {
        public override bool SendToConnection(object conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            return false;
        }

        // TODO since this won't use Steam the SteamNetworkingSend will need converting to the correct equivilent sendtype here
    }
}
