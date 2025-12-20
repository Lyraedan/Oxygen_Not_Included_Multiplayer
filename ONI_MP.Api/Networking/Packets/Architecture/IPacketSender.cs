using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

namespace ONI_MP.Api.Networking.Packets.Architecture
{
    public interface IPacketSender
    {
        // Player / host
        bool SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle);
        void SendToHost(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle);

        // Broadcast
        void SendToAll(IPacket packet, CSteamID? exclude = null, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle);
        void SendToAllClients(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle);
        void SendToAllExcluding(IPacket packet, HashSet<CSteamID> excludedIds, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle);
    }
}
