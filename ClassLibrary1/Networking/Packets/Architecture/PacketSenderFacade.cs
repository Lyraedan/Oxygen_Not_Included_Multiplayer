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
        public void SendToAll(IPacket packet, CSteamID? exclude = null, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        => PacketSender.SendToAll(packet, exclude, sendType);

        public void SendToAllClients(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        => PacketSender.SendToAllClients(packet, sendType);

        public void SendToAllExcluding(IPacket packet, HashSet<CSteamID> excludedIds, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        => PacketSender.SendToAllExcluding(packet, excludedIds, sendType);

        public void SendToHost(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        => PacketSender.SendToHost(packet, sendType);

        public bool SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        => PacketSender.SendToPlayer(steamID, packet, sendType);
    }
}
