using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

namespace ONI_MP.Api.Networking.Packets.Architecture
{
    internal class NullPacketSender : IPacketSender
    {
        public void SendToAll(IPacket packet, CSteamID? exclude = null, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle) { }

        public void SendToAllClients(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle) { }

        public void SendToAllExcluding(IPacket packet, HashSet<CSteamID> excludedIds, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle) { }

        public void SendToHost(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle) { }

        public bool SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            return false;
        }
    }
}
