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
            LAN = 1
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
                case NetworkRelay.LAN:
                    return new LanServer();
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
                case NetworkRelay.LAN:
                    return new LanClient();
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
                case NetworkRelay.LAN:
                    return new LanPacketSender();
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
                case NetworkRelay.LAN:
                    if (MultiplayerSession.IsClient)
                    {
                        return LanClient.MY_CLIENT_ID;
                    }
                    else
                    {
                        return LanServer.MY_CLIENT_ID;
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
            return relay.Equals(NetworkRelay.LAN);
        }
    }
}
