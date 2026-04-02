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
using ONI_MP.Networking.Transport.Steamworks;
using ONI_MP.DebugTools;
using Shared.Profiling;
using ONI_MP.Patches.ToolPatches;

namespace ONI_MP.Networking
{
    public static class NetworkConfig
    {
        public enum NetworkTransport
        {
            STEAMWORKS = 0,
            RIPTIDE = 1,
        }
        public static NetworkTransport transport { get; private set; } = NetworkTransport.RIPTIDE;

        public static TransportServer TransportServer { get; set; } = new RiptideServer();
        public static TransportClient TransportClient { get; set; } = new RiptideClient();
        public static TransportPacketSender TransportPacketSender { get; set; } = new RiptidePacketSender();

        /// <summary>
        /// Starts a GameServer on the current transport without needing to go through something like SteamLobby etc
        /// </summary>
        public static void StartServer()
        {
            switch(transport)
            {
                case NetworkTransport.STEAMWORKS:
                    UpdateTransport(NetworkTransport.STEAMWORKS);
                    StartSteamServer();
                    break;
                case NetworkTransport.RIPTIDE:
                    UpdateTransport(NetworkTransport.RIPTIDE);
                    StartRawServer();
                    break;
            }
        }

        private static void StartSteamServer()
        {
            SteamLobby.CreateLobby(onSuccess: () =>
            {
                SpeedControlScreen.Instance?.Unpause(false);
                Game.Instance.Trigger(MP_HASHES.OnMultiplayerGameSessionInitialized);
            });
        }

        private static void StartRawServer()
        {
            MultiplayerSession.Clear();
            try
            {
                Networking.GameServer.Start();
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Failed to start LAN game server: {ex.Message}");
            }
            SelectToolPatch.UpdateColor();
            Game.Instance.Trigger(MP_HASHES.OnMultiplayerGameSessionInitialized);
        }

        public static void StopServer()
        {
            switch(transport)
            {
                case NetworkTransport.STEAMWORKS:
                    StopSteamServer();
                    break;
                case NetworkTransport.RIPTIDE:
                    StopRawServer();
                    break;
            }
        }

        private static void StopSteamServer()
        {
            SteamLobby.LeaveLobby();
        }

        private static void StopRawServer()
        {
            if (MultiplayerSession.IsHost)
                GameServer.Shutdown();

            if (MultiplayerSession.IsClient)
                GameClient.Disconnect();

            NetworkIdentityRegistry.Clear();
            MultiplayerSession.Clear();

            SelectToolPatch.UpdateColor();
        }

        public static void UpdateTransport(NetworkTransport newTransport)
        {
            if (transport.Equals(newTransport))
                return;
            
            transport = newTransport;
            TransportServer = GetTransportServer();
            TransportClient = GetTransportClient();
            TransportPacketSender = GetTransportPacketSender();
            DebugConsole.Log($"Updated network transport to: {newTransport.ToString()}");
        }

        public static TransportServer GetTransportServer()
        {
            using var _ = Profiler.Scope();

            switch (transport)
            {
                case NetworkTransport.STEAMWORKS:
                    return new SteamServer();
                case NetworkTransport.RIPTIDE:
                    return new RiptideServer();
                default:
                    return new RiptideServer(); // Use riptide by default now
            }
        }

        public static TransportClient GetTransportClient()
        {
            using var _ = Profiler.Scope();

            switch (transport)
            {
                case NetworkTransport.STEAMWORKS:
                    return new SteamClient();
                case NetworkTransport.RIPTIDE:
                    return new RiptideClient();
                default:
                    return new RiptideClient(); // Use riptide by default now
            }
        }

        public static TransportPacketSender GetTransportPacketSender()
        {
            using var _ = Profiler.Scope();

            switch (transport)
            {
                case NetworkTransport.STEAMWORKS:
                    return new SteamworksPacketSender();
                case NetworkTransport.RIPTIDE:
                    return new RiptidePacketSender();
                default:
                    return new RiptidePacketSender(); // Use riptide by default now
            }
        }

        public static ulong GetLocalID()
        {
            using var _ = Profiler.Scope();

            switch (transport)
            {
                case NetworkTransport.STEAMWORKS:
                    return SteamUser.GetSteamID().m_SteamID;
                case NetworkTransport.RIPTIDE:
                    if (MultiplayerSession.IsClient)
                    {
                        return RiptideClient.CLIENT_ID;
                    }
                    else
                    {
                        return RiptideServer.CLIENT_ID;
                    }
                default:
                    return Utils.NilUlong();
            }
        }

        public static bool IsSteamConfig()
        {
            using var _ = Profiler.Scope();

            return transport.Equals(NetworkTransport.STEAMWORKS);
        }

        public static bool IsLanConfig()
        {
            using var _ = Profiler.Scope();

            return transport.Equals(NetworkTransport.RIPTIDE);
        }

        public static List<ulong> GetConnectedClients()
        {
            using var _ = Profiler.Scope();

            List<ulong> clients = new List<ulong>();
            switch(transport)
            {
                case NetworkTransport.STEAMWORKS:
                    List<CSteamID> members = SteamLobby.GetAllLobbyMembers();
                    foreach(CSteamID member in members)
                    {
                        clients.Add(member.m_SteamID);
                    }
                    break;
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
            }
            return clients;
        }
    }
}

