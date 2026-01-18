using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Misc;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.Relay.Lan;
using ONI_MP.Networking.Relay.Steam;
using Steamworks;
using SteamServer = ONI_MP.Networking.Relay.Steam.SteamServer;
using SteamClient = ONI_MP.Networking.Relay.Steam.SteamClient;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking
{
    public static class NetworkConfig
    {
        public enum NetworkRelay
        {
            STEAM = 0,
            RIPTIDE = 1,
            LITENETLIB = 2,
        }
        public static NetworkRelay relay { get; private set; } = NetworkRelay.STEAM;

        public static RelayServer RelayServer { get; set; } = new SteamServer();
        public static RelayClient RelayClient { get; set; } = new SteamClient();
        public static RelayPacketSender RelayPacketSender { get; set; } = new SteamPacketSender();

        public static void UpdateRelay(NetworkRelay newRelay)
        {
            relay = newRelay;
            RelayServer = GetRelayServer();
            RelayClient = GetRelayClient();
            RelayPacketSender = GetRelayPacketSender();
            DebugConsole.Log($"Updated network relay to: {newRelay.ToString()}");
        }

        public static RelayServer GetRelayServer()
        {
            switch (relay)
            {
                case NetworkRelay.STEAM:
                    return new SteamServer();
                case NetworkRelay.RIPTIDE:
                    return new RiptideServer();
                case NetworkRelay.LITENETLIB:
                    return new LiteNetLibServer();
                default:
                    return new SteamServer();
            }
        }

        public static RelayClient GetRelayClient()
        {
            switch (relay)
            {
                case NetworkRelay.STEAM:
                    return new SteamClient();
                case NetworkRelay.RIPTIDE:
                    return new RiptideClient();
                case NetworkRelay.LITENETLIB:
                    return new LiteNetLibClient();
                default:
                    return new SteamClient();
            }
        }

        public static RelayPacketSender GetRelayPacketSender()
        {
            switch (relay)
            {
                case NetworkRelay.STEAM:
                    return new SteamPacketSender();
                case NetworkRelay.RIPTIDE:
                    return new RiptidePacketSender();
                default:
                    return new SteamPacketSender();
            }
        }
    
        public static ulong GetLocalID()
        {
            switch (relay)
            {
                case NetworkRelay.STEAM:
                    return SteamUser.GetSteamID().m_SteamID;
                case NetworkRelay.RIPTIDE:
                    if (MultiplayerSession.IsClient)
                    {
                        RiptideClient client = RelayClient as RiptideClient;
                        return client.GetClientID();
                    }
                    else
                    {
                        RiptideServer server = RelayServer as RiptideServer;
                        return server.GetClientID();
                    }
                case NetworkRelay.LITENETLIB:
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
            return relay.Equals(NetworkRelay.STEAM);
        }

        public static bool IsLanConfig()
        {
            return relay.Equals(NetworkRelay.RIPTIDE) || relay.Equals(NetworkRelay.LITENETLIB);
        }
    }
}
