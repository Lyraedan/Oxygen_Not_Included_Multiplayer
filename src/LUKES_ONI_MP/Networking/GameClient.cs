using Steamworks;
using ONI_MP.DebugTools;
using System;
using System.Runtime.InteropServices;
using ONI_MP.Networking.Packets;
using ONI_MP.Misc;
using ONI_MP.Menus;
using ONI_MP.Networking.States;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using System.Collections;
using UnityEngine;
using ONI_MP.Patches.ToolPatches;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Networking
{
    public static class GameClient
    {
        private static Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;
        public static HSteamNetConnection? Connection { get; private set; }

        private static ClientState _state = ClientState.Disconnected;
        public static ClientState State => _state;

        private static bool _pollingPaused = false;

        private static CachedConnectionInfo? _cachedConnectionInfo = null;

        public static bool IsHardSyncInProgress = false;

        private static bool userTriggeredDisconnect = false;

        private struct CachedConnectionInfo
        {
            public CSteamID HostSteamID;

            public CachedConnectionInfo(CSteamID id)
            {
                HostSteamID = id;
            }
        }

        public static void SetState(ClientState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                DebugConsole.Log($"[GameClient] State changed to: {_state}");
            }
        }

        public static void Init()
        {
            if (_connectionStatusChangedCallback == null)
            {
                _connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
                DebugConsole.Log("[GameClient] Registered connection status callback.");
            }
        }

        public static void ConnectToHost(CSteamID hostSteamId, bool showLoadingScreen = true)
        {
            if(showLoadingScreen)
            {
                MultiplayerOverlay.Show($"Connecting to {SteamFriends.GetFriendPersonaName(hostSteamId)}!");
            }

            DebugConsole.Log($"[GameClient] Attempting ConnectP2P to host {hostSteamId}...");
            SetState(ClientState.Connecting);

            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID64(hostSteamId.m_SteamID);

            Connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, null);
            DebugConsole.Log($"[GameClient] ConnectP2P returned handle: {Connection.Value.m_HSteamNetConnection}");
        }

        public static void Disconnect()
        {
            if (Connection.HasValue)
            {
                userTriggeredDisconnect = true;
                DebugConsole.Log("[GameClient] Disconnecting from host...");

                bool result = SteamNetworkingSockets.CloseConnection(
                    Connection.Value,
                    0,
                    "Client disconnecting",
                    false
                );

                DebugConsole.Log($"[GameClient] CloseConnection result: {result}");
                Connection = null;
                SetState(ClientState.Disconnected);
                MultiplayerSession.InSession = false;
            }
            else
            {
                DebugConsole.LogWarning("[GameClient] Disconnect called, but no connection exists.");
            }
        }

        public static void ReconnectToSession()
        {
            if (Connection.HasValue || State == ClientState.Connected || State == ClientState.Connecting)
            {
                DebugConsole.Log("[GameClient] Reconnecting: First disconnecting existing connection.");
                Disconnect();
                System.Threading.Thread.Sleep(100);
            }

            if (MultiplayerSession.HostSteamID != CSteamID.Nil)
            {
                DebugConsole.Log("[GameClient] Attempting to reconnect to host...");
                ConnectToHost(MultiplayerSession.HostSteamID);
            }
            else
            {
                DebugConsole.LogWarning("[GameClient] Cannot reconnect: HostSteamID is not set.");
            }
        }

        public static void Poll()
        {
            if (_pollingPaused)
                return;

            SteamNetworkingSockets.RunCallbacks();

            switch (State)
            {
                case ClientState.Connected:
                case ClientState.InGame:
                    if (Connection.HasValue)
                        ProcessIncomingMessages(Connection.Value);
                    break;
                case ClientState.Connecting:
                case ClientState.Disconnected:
                case ClientState.Error:
                default:
                    break;
            }
        }

        private static void ProcessIncomingMessages(HSteamNetConnection conn)
        {
            int maxMessagesPerConnectionPoll = Configuration.GetClientProperty<int>("MaxMessagesPerPoll");
            IntPtr[] messages = new IntPtr[maxMessagesPerConnectionPoll];
            int msgCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, messages, maxMessagesPerConnectionPoll);

            for (int i = 0; i < msgCount; i++)
            {
                var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(messages[i]);
                byte[] data = new byte[msg.m_cbSize];
                Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);

                try
                {
                    PacketHandler.HandleIncoming(data);
                }
                catch (Exception ex)
                {
                    DebugConsole.LogError($"[GameClient] Failed to handle incoming packet: {ex}", false); // I'm sick and tired of you crashing the game
                }

                SteamNetworkingMessage_t.Release(messages[i]);
            }
        }

        private static void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
        {
            var state = data.m_info.m_eState;
            var remote = data.m_info.m_identityRemote.GetSteamID();

            DebugConsole.Log($"[GameClient] Connection status changed: {state} (remote={remote})");

            if (Connection.HasValue && data.m_hConn.m_HSteamNetConnection != Connection.Value.m_HSteamNetConnection)
                return;

            switch (state)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    OnConnected();
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    OnDisconnected("Closed by peer or problem detected locally", remote, state);
                    break;
                default:
                    break;
            }
        }

        private static void OnConnected()
        {
            //MultiplayerOverlay.Close();
            SetState(ClientState.Connected);

            // We've reconnected in game
            MultiplayerSession.InSession = true;

            var hostId = MultiplayerSession.HostSteamID;
            if (!MultiplayerSession.ConnectedPlayers.ContainsKey(hostId))
            {
                var hostPlayer = new MultiplayerPlayer(hostId);
                MultiplayerSession.ConnectedPlayers[hostId] = hostPlayer;
            }

            // Store the connection handle for host
            MultiplayerSession.ConnectedPlayers[hostId].Connection = Connection;

            DebugConsole.Log("[GameClient] Connection to host established!");
            if (Utils.IsInMenu())
            {
                MultiplayerOverlay.Show($"Waiting for {SteamFriends.GetFriendPersonaName(MultiplayerSession.HostSteamID)}...");
                if (!IsHardSyncInProgress)
                {
                    var packet = new SaveFileRequestPacket
                    {
                        Requester = MultiplayerSession.LocalSteamID
                    };
                    PacketSender.SendToHost(packet);
                } else
                {
                    // Tell the host we're ready
                    ReadyManager.SendReadyStatusPacket(ClientReadyState.Ready);
                }
            } 
            else if(Utils.IsInGame())
            {
                // We're in game already. Consider this a reconnection
                SetState(ClientState.InGame);
                PacketHandler.readyToProcess = true;
                if (IsHardSyncInProgress)
                    IsHardSyncInProgress = false;

                ReadyManager.SendReadyStatusPacket(ClientReadyState.Ready);
                MultiplayerSession.CreateConnectedPlayerCursors();
                SelectToolPatch.UpdateColor();
            }
        }

        private static void OnDisconnected(string reason, CSteamID remote, ESteamNetworkingConnectionState state)
        {
            DebugConsole.LogWarning($"[GameClient] Connection closed or failed ({state}) for {remote}. Reason: {reason}");
            MultiplayerSession.InSession = false;
            SetState(ClientState.Disconnected);
            if(!userTriggeredDisconnect && remote == MultiplayerSession.HostSteamID)
            {
                CoroutineRunner.RunOne(ShowMessageAndReturnToTitle());
            }
            userTriggeredDisconnect = false;
            Connection = null;
        }

        private static IEnumerator ShowMessageAndReturnToTitle()
        {
            MultiplayerOverlay.Show("Connection to the host was lost!");
            yield return new WaitForSeconds(3f);
            PauseScreen.TriggerQuitGame(); // Force exit to frontend

            MultiplayerOverlay.Close();
            NetworkIdentityRegistry.Clear();
            SteamLobby.LeaveLobby();
        }

        public static int? GetPingToHost()
        {
            if (Connection.HasValue)
            {
                SteamNetConnectionRealTimeStatus_t status = default;
                SteamNetConnectionRealTimeLaneStatus_t laneStatus = default;

                EResult res = SteamNetworkingSockets.GetConnectionRealTimeStatus(
                    Connection.Value,
                    ref status,
                    0,
                    ref laneStatus
                );

                if (res == EResult.k_EResultOK)
                {
                    return status.m_nPing >= 0 ? (int?)status.m_nPing : null;
                }
            }
            return null;
        }

        public static void CacheCurrentServer()
        {
            if (MultiplayerSession.HostSteamID != CSteamID.Nil)
            {
                _cachedConnectionInfo = new CachedConnectionInfo(
                    MultiplayerSession.HostSteamID
                );
                DebugConsole.Log($"[GameClient] Cached server: {_cachedConnectionInfo.Value.HostSteamID}");
            }
            else
            {
                DebugConsole.LogWarning("[GameClient] Tried to cache, but HostSteamID is Nil.");
            }
        }

        public static void ReconnectFromCache()
        {
            if (_cachedConnectionInfo.HasValue)
            {
                DebugConsole.Log($"[GameClient] Reconnecting to cached server: {_cachedConnectionInfo.Value.HostSteamID}");
                ConnectToHost(_cachedConnectionInfo.Value.HostSteamID, false);
            }
            else
            {
                DebugConsole.LogWarning("[GameClient] No cached server info available to reconnect.");
            }
        }



        public static void PauseNetworkingCallbacks()
        {
            _pollingPaused = true;
            DebugConsole.Log("[GameClient] Networking callbacks paused.");
        }

        public static void ResumeNetworkingCallbacks()
        {
            _pollingPaused = false;
            DebugConsole.Log("[GameClient] Networking callbacks resumed.");
        }

        public static void DisableMessageHandlers()
        {
            if (_connectionStatusChangedCallback != null)
            {
                _connectionStatusChangedCallback.Unregister();
                _connectionStatusChangedCallback = null;
                DebugConsole.Log("[GameClient] Networking message handlers disabled.");
            }
        }

        public static void EnableMessageHandlers()
        {
            if (_connectionStatusChangedCallback == null)
            {
                _connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
                DebugConsole.Log("[GameClient] Networking message handlers enabled.");
            }
        }
    }
}
