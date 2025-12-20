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
        public bool SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType)
        {
            return false;
        }

        public void SendToHost(IPacket packet, SteamNetworkingSend sendType) { }

        public void SendToAll(IPacket packet, SteamNetworkingSend sendType) { }

        public void SendToAll(IPacket packet, CSteamID exclude, SteamNetworkingSend sendType) { }

        public void SendToAllClients(IPacket packet, SteamNetworkingSend sendType) { }

        public void SendToAllExcluding(IPacket packet, HashSet<CSteamID> excludedIds, SteamNetworkingSend sendType) { }
    }
}
