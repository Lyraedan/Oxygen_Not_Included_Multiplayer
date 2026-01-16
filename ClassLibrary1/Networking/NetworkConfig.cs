using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.Relay.Steam;

namespace ONI_MP.Networking
{
    public static class NetworkConfig
    {
        public enum NetworkRelay
        {
            STEAM,
            LAN
        }
        public static NetworkRelay relay = NetworkRelay.STEAM;

        public static RelayServer RelayServer { get; set; } = new SteamServer();
        public static RelayClient RelayClient { get; set; } = new SteamClient();
        public static RelayPacketSender RelayPacketSender { get; set; } = new SteamPacketSender();

        public static RelayServer GetRelayServer()
        {
            switch (relay)
            {
                case NetworkRelay.STEAM:
                    return new SteamServer();
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
                default:
                    return new SteamPacketSender();
            }
        }
    }
}
