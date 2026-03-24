using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Misc;
using ONI_MP.Networking.Transport;
using ONI_MP.Networking.Transport.Lan;
#if STEAM_WORKSHOP_VERSION
using ONI_MP.Networking.Transport.Steam;
using Steamworks;
using SteamServer = ONI_MP.Networking.Transport.Steam.SteamworksServer;
using SteamClient = ONI_MP.Networking.Transport.Steam.SteamworksClient;
using ONI_MP.Networking.Transport.Steamworks;
#endif
using ONI_MP.DebugTools;

namespace ONI_MP.Networking
{
    public static class NetworkConfig
    {
        public enum NetworkTransport
        {
            STEAMWORKS = 0,
            RIPTIDE = 1,
            LITENETLIB = 2, // Non functional right now
        }
        public static NetworkTransport transport { get; private set; } = NetworkTransport.STEAMWORKS;

        public static TransportServer TransportServer { get; set; } = new RiptideServer();
        public static TransportClient TransportClient { get; set; } = new RiptideClient();
        public static TransportPacketSender TransportPacketSender { get; set; } = new RiptidePacketSender();

        public static void UpdateTransport(NetworkTransport newTransport)
        {
            if (newTransport == NetworkTransport.STEAMWORKS)
            {
#if !STEAM_WORKSHOP_VERSION
                newTransport = NetworkTransport.RIPTIDE; // Steam workshop is not enabled. Fallback to Riptide
#endif
            }
            transport = newTransport;
            TransportServer = GetTransportServer();
            TransportClient = GetTransportClient();
            TransportPacketSender = GetTransportPacketSender();
            DebugConsole.Log($"Updated network transport to: {newTransport.ToString()}");
        }

        public static TransportServer GetTransportServer()
        {
            switch (transport)
            {
#if STEAM_WORKSHOP_VERSION
                case NetworkTransport.STEAMWORKS:
                    return new SteamServer();
#endif
                case NetworkTransport.RIPTIDE:
                    return new RiptideServer();
                case NetworkTransport.LITENETLIB:
                    return new LiteNetLibServer();
                default:
                    return new RiptideServer(); // Use riptide by default now
            }
        }

        public static TransportClient GetTransportClient()
        {
            switch (transport)
            {
#if STEAM_WORKSHOP_VERSION
                case NetworkTransport.STEAMWORKS:
                    return new SteamClient();
#endif
                case NetworkTransport.RIPTIDE:
                    return new RiptideClient();
                case NetworkTransport.LITENETLIB:
                    return new LiteNetLibClient();
                default:
                    return new RiptideClient(); // Use riptide by default now
            }
        }

        public static TransportPacketSender GetTransportPacketSender()
        {
            switch (transport)
            {
#if STEAM_WORKSHOP_VERSION
                case NetworkTransport.STEAMWORKS:
                    return new SteamworksPacketSender();
#endif
                case NetworkTransport.RIPTIDE:
                    return new RiptidePacketSender();
                case NetworkTransport.LITENETLIB:
                    return new LiteNetLibPacketSender();
                default:
                    return new RiptidePacketSender(); // Use riptide by default now
            }
        }
    
        public static ulong GetLocalID()
        {
            switch (transport)
            {
#if STEAM_WORKSHOP_VERSION
                case NetworkTransport.STEAMWORKS:
                    return SteamUser.GetSteamID().m_SteamID;
#endif
                case NetworkTransport.RIPTIDE:
                    if (MultiplayerSession.IsClient)
                    {
                        return RiptideClient.CLIENT_ID;
                    }
                    else
                    {
                        return RiptideServer.CLIENT_ID;
                    }
                case NetworkTransport.LITENETLIB:
                    if (MultiplayerSession.IsClient)
                    {
                        return LiteNetLibClient.MY_CLIENT_ID;
                    }
                    else
                    {
                        return LiteNetLibClient.MY_CLIENT_ID;
                    }
                default:
                    return Utils.NilUlong();
            }
        }

        public static bool IsSteamConfig()
        {
            return transport.Equals(NetworkTransport.STEAMWORKS);
        }

        public static bool IsLanConfig()
        {
            return transport.Equals(NetworkTransport.RIPTIDE) || transport.Equals(NetworkTransport.LITENETLIB);
        }
    
        public static List<ulong> GetConnectedClients()
        {
            List<ulong> clients = new List<ulong>();
            switch(transport)
            {
#if STEAM_WORKSHOP_VERSION
                case NetworkTransport.STEAMWORKS:
                    List<CSteamID> members = SteamLobby.GetAllLobbyMembers();
                    foreach(CSteamID member in members)
                    {
                        clients.Add(member.m_SteamID);
                    }
                    break;
#endif
                case NetworkTransport.RIPTIDE:
                    if (MultiplayerSession.IsClient)
                    {
                        RiptideClient client = TransportClient as RiptideClient;
                        return client.ClientList;
                    }
                    else
                    {
                        RiptideServer server = TransportServer as RiptideServer;
                        return server.ClientList;
                    }
                case NetworkTransport.LITENETLIB:
                    break;
            }
            return clients;
        }
    }
}
