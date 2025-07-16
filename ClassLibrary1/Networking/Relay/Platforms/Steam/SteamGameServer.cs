using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.States;
using Steamworks;

namespace ONI_MP.Networking.Relay.Platforms.Steam
{
    public class SteamGameServer : IGameServer
    {
        private HSteamListenSocket _listenSocket;
        private HSteamNetPollGroup _pollGroup;
        private Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;

        private ServerState _state = ServerState.Stopped;
        public ServerState State => _state;

        private void SetState(ServerState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                DebugConsole.Log($"[SteamGameServer] State changed to: {_state}");
            }
        }

        public void Start()
        {
            SetState(ServerState.Preparing);

            if (!SteamManager.Initialized)
            {
                SetState(ServerState.Error);
                DebugConsole.LogError("[SteamGameServer] SteamManager not initialized!");
                return;
            }

            SetState(ServerState.Starting);

            _listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
            if (_listenSocket.m_HSteamListenSocket == 0)
            {
                SetState(ServerState.Error);
                DebugConsole.LogError("[SteamGameServer] Failed to create ListenSocket!");
                return;
            }

            _pollGroup = SteamNetworkingSockets.CreatePollGroup();
            if (_pollGroup.m_HSteamNetPollGroup == 0)
            {
                SetState(ServerState.Error);
                DebugConsole.LogError("[SteamGameServer] Failed to create PollGroup!");
                SteamNetworkingSockets.CloseListenSocket(_listenSocket);
                return;
            }

            _connectionStatusChangedCallback =
                Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);

            DebugConsole.Log("[SteamGameServer] Listen socket and poll group created.");
            MultiplayerSession.InSession = true;

            SetState(ServerState.Started);
        }

        public void Shutdown()
        {
            SetState(ServerState.Stopped);

            foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
            {
                if (player.Connection is SteamConnection steamConn)
                {
                    SteamNetworkingSockets.CloseConnection(steamConn.Handle, 0, "Shutdown", false);
                    player.Connection = null;
                }
            }


            if (_pollGroup.m_HSteamNetPollGroup != 0)
                SteamNetworkingSockets.DestroyPollGroup(_pollGroup);

            if (_listenSocket.m_HSteamListenSocket != 0)
                SteamNetworkingSockets.CloseListenSocket(_listenSocket);

            MultiplayerSession.InSession = false;
            DebugConsole.Log("[SteamGameServer] Shutdown complete.");
        }

        public void Update()
        {
            if (State != ServerState.Started)
                return;

            SteamAPI.RunCallbacks();
            SteamNetworkingSockets.RunCallbacks();
            ReceiveMessages();
        }

        private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
        {
            var conn = data.m_hConn;
            var clientId = data.m_info.m_identityRemote.GetSteamID();
            var state = data.m_info.m_eState;

            DebugConsole.Log($"[SteamGameServer] Connection state={state} from {clientId}");

            switch (state)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                    TryAcceptConnection(conn, clientId);
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    OnClientConnected(conn, clientId);
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    OnClientClosed(conn, clientId);
                    break;
            }
        }

        private void TryAcceptConnection(HSteamNetConnection conn, CSteamID clientId)
        {
            var result = SteamNetworkingSockets.AcceptConnection(conn);
            if (result == EResult.k_EResultOK)
            {
                SteamNetworkingSockets.SetConnectionPollGroup(conn, _pollGroup);
                DebugConsole.Log($"[SteamGameServer] Accepted connection from {clientId}");
            }
            else
            {
                DebugConsole.LogError($"[SteamGameServer] Rejected connection from {clientId}: {result}");
                SteamNetworkingSockets.CloseConnection(conn, 0, "Accept failed", false);
            }
        }

        private void OnClientConnected(HSteamNetConnection conn, CSteamID clientId)
        {
            string idStr = clientId.m_SteamID.ToString();

            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(idStr, out var player))
            {
                player = new MultiplayerPlayer(idStr);
                MultiplayerSession.ConnectedPlayers[idStr] = player;
            }

            player.Connection = new SteamConnection(clientId, conn);

            DebugConsole.Log($"[SteamGameServer] Connection to {clientId} fully established!");
        }


        private void OnClientClosed(HSteamNetConnection conn, CSteamID clientId)
        {
            SteamNetworkingSockets.CloseConnection(conn, 0, null, false);
            string idStr = clientId.m_SteamID.ToString();

            if (MultiplayerSession.ConnectedPlayers.TryGetValue(idStr, out var player))
            {
                player.Connection = null;
            }

            DebugConsole.Log($"[SteamGameServer] Connection closed for {clientId}");
        }

        private void ReceiveMessages()
        {
            int max = Configuration.GetHostProperty<int>("MaxMessagesPerPoll");
            var messages = new IntPtr[max];
            int count = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(_pollGroup, messages, max);

            for (int i = 0; i < count; i++)
            {
                var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(messages[i]);
                byte[] data = new byte[msg.m_cbSize];
                Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);
                PacketHandler.HandleIncoming(data);
                SteamNetworkingMessage_t.Release(messages[i]);
            }
        }
    }
}
