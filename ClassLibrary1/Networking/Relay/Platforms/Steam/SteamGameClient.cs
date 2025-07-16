using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.Relay.Platforms.Steam;
using ONI_MP.Networking.States;
using Steamworks;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ONI_MP.Networking.Platforms.Steam
{
    public class SteamGameClient : IGameClient
    {
        private HSteamNetConnection? _connection;
        private ClientState _state = ClientState.Disconnected;
        private CSteamID _hostSteamId = CSteamID.Nil;
        private bool _isHardSyncInProgress = false;
        private CSteamID _cachedHost = CSteamID.Nil;

        public ClientState State => _state;
        public bool IsConnected => _state == ClientState.Connected || _state == ClientState.InGame;

        public bool IsHardSyncInProgress
        {
            get => _isHardSyncInProgress;
            set => _isHardSyncInProgress = value;
        }

        public void Init()
        {
            // Steam API callbacks are already registered globally elsewhere
            DebugConsole.Log("[SteamGameClient] Init() called.");
        }

        public void ConnectToHost(string id, bool showLoadingScreen = true)
        {
            if (!ulong.TryParse(id, out var steamIdRaw))
            {
                DebugConsole.LogError($"[GameClient] Invalid Steam ID: {id}");
                return;
            }

            _hostSteamId = new CSteamID(steamIdRaw);

            if (showLoadingScreen)
            {
                MultiplayerOverlay.Show($"Connecting to {SteamFriends.GetFriendPersonaName(_hostSteamId)}...");
            }

            DebugConsole.Log($"[SteamGameClient] Connecting to host: {_hostSteamId}");

            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID64(_hostSteamId.m_SteamID);
            _connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, null);

            SetState(ClientState.Connecting);
        }

        public void ReconnectToSession()
        {
            Disconnect();
            System.Threading.Thread.Sleep(100);

            if (_hostSteamId != CSteamID.Nil)
            {
                DebugConsole.Log("[SteamGameClient] Reconnecting to session.");
                ConnectToHost(_hostSteamId.ToString());
            }
            else
            {
                DebugConsole.LogWarning("[SteamGameClient] HostSteamID is nil; cannot reconnect.");
            }
        }

        public void Disconnect()
        {
            if (_connection.HasValue)
            {
                SteamNetworkingSockets.CloseConnection(_connection.Value, 0, "Client disconnect", false);
                _connection = null;
                _state = ClientState.Disconnected;
            }

            MultiplayerSession.InSession = false;
        }

        public void Poll()
        {
            SteamNetworkingSockets.RunCallbacks();

            if (_connection.HasValue && (State == ClientState.Connected || State == ClientState.InGame))
            {
                ProcessIncomingMessages(_connection.Value);
            }
        }

        public INetworkConnection GetHostConnection()
        {
            return _connection.HasValue ? new SteamConnection(_hostSteamId, _connection.Value) : null;
        }

        public int? GetPingToHost()
        {
            if (_connection.HasValue)
            {
                SteamNetConnectionRealTimeStatus_t status = default;
                SteamNetConnectionRealTimeLaneStatus_t laneStatus = default;

                EResult res = SteamNetworkingSockets.GetConnectionRealTimeStatus(
                    _connection.Value, ref status, 0, ref laneStatus);

                if (res == EResult.k_EResultOK && status.m_nPing >= 0)
                    return status.m_nPing;
            }

            return -1;
        }

        public void CacheCurrentServer()
        {
            _cachedHost = _hostSteamId;
            DebugConsole.Log($"[SteamGameClient] Cached host: {_cachedHost}");
        }

        public void ReconnectFromCache()
        {
            if (_cachedHost != CSteamID.Nil)
            {
                DebugConsole.Log($"[SteamGameClient] Reconnecting from cache: {_cachedHost}");
                ConnectToHost(_cachedHost.ToString());
            }
            else
            {
                DebugConsole.LogWarning("[SteamGameClient] No cached host to reconnect to.");
            }
        }

        public void SetState(ClientState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                DebugConsole.Log($"[SteamGameClient] State changed to: {_state}");
            }
        }

        private void ProcessIncomingMessages(HSteamNetConnection conn)
        {
            int maxMessages = Configuration.GetClientProperty<int>("MaxMessagesPerPoll");
            IntPtr[] messages = new IntPtr[maxMessages];
            int count = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, messages, maxMessages);

            for (int i = 0; i < count; i++)
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
                    DebugConsole.LogError($"[SteamGameClient] Failed to handle packet: {ex}");
                }

                SteamNetworkingMessage_t.Release(messages[i]);
            }
        }
    }
}
