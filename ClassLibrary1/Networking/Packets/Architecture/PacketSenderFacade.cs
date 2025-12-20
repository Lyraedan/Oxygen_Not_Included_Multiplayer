using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Api.Networking;
using ONI_MP.Api.Networking.Packets.Architecture;
using Steamworks;

namespace ONI_MP.Networking.Packets.Architecture
{
    internal sealed class PacketSenderFacade : IPacketSender
    {
        public bool SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType)
            => PacketSender.SendToPlayer(steamID, packet, sendType);

        public void SendToHost(IPacket packet, SteamNetworkingSend sendType)
            => PacketSender.SendToHost(packet, sendType);

        public void SendToAll(IPacket packet, SteamNetworkingSend sendType)
            => PacketSender.SendToAll(packet, null, sendType);

        public void SendToAll(IPacket packet, CSteamID exclude, SteamNetworkingSend sendType)
            => PacketSender.SendToAll(packet, exclude, sendType);

        public void SendToAllClients(IPacket packet, SteamNetworkingSend sendType)
            => PacketSender.SendToAllClients(packet, sendType);

        public void SendToAllExcluding(IPacket packet, HashSet<CSteamID> excludedIds, SteamNetworkingSend sendType)
            => PacketSender.SendToAllExcluding(packet, excludedIds, sendType);
    }
}
