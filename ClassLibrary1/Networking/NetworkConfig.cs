using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Misc;
using ONI_MP.Networking.Transport;
using ONI_MP.Networking.Transport.Lan;
using ONI_MP.Networking.Transport.Steam;
using Steamworks;
using SteamServer = ONI_MP.Networking.Transport.Steam.SteamworksServer;
using SteamClient = ONI_MP.Networking.Transport.Steam.SteamworksClient;
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

        public static TransportServer TransportServer { get; set; } = new SteamServer();
        public static TransportClient TransportClient { get; set; } = new SteamClient();
        public static TransportPacketSender TransportPacketSender { get; set; } = new SteamworksPacketSender();

        public static void UpdateTransport(NetworkTransport newTransport)
        {
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
                case NetworkTransport.STEAMWORKS:
                    return new SteamServer();
                case NetworkTransport.RIPTIDE:
                    return new RiptideServer();
                case NetworkTransport.LITENETLIB:
                    return new LiteNetLibServer();
                default:
                    return new SteamServer();
            }
        }

        public static TransportClient GetTransportClient()
        {
            switch (transport)
            {
                case NetworkTransport.STEAMWORKS:
                    return new SteamClient();
                case NetworkTransport.RIPTIDE:
                    return new RiptideClient();
                case NetworkTransport.LITENETLIB:
                    return new LiteNetLibClient();
                default:
                    return new SteamClient();
            }
        }

        public static TransportPacketSender GetTransportPacketSender()
        {
            switch (transport)
            {
                case NetworkTransport.STEAMWORKS:
                    return new SteamworksPacketSender();
                case NetworkTransport.RIPTIDE:
                    return new RiptidePacketSender();
                case NetworkTransport.LITENETLIB:
                    return new LiteNetLibPacketSender();
                default:
                    return new SteamworksPacketSender();
            }
        }
    
        public static ulong GetLocalID()
        {
            switch (transport)
            {
                case NetworkTransport.STEAMWORKS:
                    return SteamUser.GetSteamID().m_SteamID;
                case NetworkTransport.RIPTIDE:
                    if (MultiplayerSession.IsClient)
                    {
                        RiptideClient client = TransportClient as RiptideClient;
                        return client.GetClientID();
                    }
                    else
                    {
                        RiptideServer server = TransportServer as RiptideServer;
                        return server.GetClientID();
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
    }
}
