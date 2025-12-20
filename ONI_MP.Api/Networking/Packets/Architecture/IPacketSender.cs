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
        bool SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType);
        void SendToHost(IPacket packet, SteamNetworkingSend sendType);

        // Broadcast
        void SendToAll(IPacket packet, SteamNetworkingSend sendType);
        void SendToAll(IPacket packet, CSteamID exclude, SteamNetworkingSend sendType);
        void SendToAllClients(IPacket packet, SteamNetworkingSend sendType);
        void SendToAllExcluding(IPacket packet, HashSet<CSteamID> excludedIds, SteamNetworkingSend sendType);
    }
}
