using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Handshake;
using ONI_MP.Networking.Profiling;
using ONI_MP.Networking.States;
using Shared;
using Steamworks;

namespace ONI_MP.Networking.Relay.Steam
{
    public class SteamClient : RelayClient
    {
        private static Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;
        public static HSteamNetConnection? Connection { get; private set; }

        private static SteamNetConnectionRealTimeStatus_t? connectionHealth = null;

        public override void Prepare()
        {
            if (_connectionStatusChangedCallback == null)
            {
                _connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
                DebugConsole.Log("[GameClient] Registered connection status callback.");
            }
        }

        public override void ConnectToHost()
        {
            ulong hostSteamId = MultiplayerSession.HostSteamID;
            DebugConsole.Log($"[GameClient] Attempting ConnectP2P to host {hostSteamId}...");

            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID64(hostSteamId);

            Connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, null);
            DebugConsole.Log($"[GameClient] ConnectP2P returned handle: {Connection.Value.m_HSteamNetConnection}");
        }

        public override void Disconnect()
        {
            if (Connection.HasValue)
            {
                DebugConsole.Log("[GameClient] Disconnecting from host...");

                bool result = SteamNetworkingSockets.CloseConnection(
                        Connection.Value,
                        0,
                        "Client disconnecting",
                        false
                );

                DebugConsole.Log($"[GameClient] CloseConnection result: {result}");
                Connection = null;
                
                MultiplayerSession.InSession = false;
                //SaveHelper.CaptureWorldSnapshot();
            }
            else
            {
                DebugConsole.LogWarning("[GameClient] Disconnect called, but no connection exists.");
            }
        }

        public override void ReconnectToSession()
        {
            if (Connection.HasValue || GameClient.State == ClientState.Connected || GameClient.State == ClientState.Connecting) // TODO FIX
            {
                DebugConsole.Log("[GameClient] Reconnecting: First disconnecting existing connection.");
                Disconnect();
                System.Threading.Thread.Sleep(100);
            }

            if (MultiplayerSession.HostSteamID != Utils.NilUlong())
            {
                DebugConsole.Log("[GameClient] Attempting to reconnect to host...");
                //ConnectToHost(MultiplayerSession.HostSteamID);
                ConnectToHost();
            }
            else
            {
                DebugConsole.LogWarning("[GameClient] Cannot reconnect: HostSteamID is not set.");
            }
        }

        public override void Update()
        {
            SteamNetworkingSockets.RunCallbacks();
            EvaluateConnectionHealth();
        }

        public override void OnMessageRecieved()
        {
            if (Connection.HasValue)
                ProcessIncomingMessages(Connection.Value);
            //else
            //    DebugConsole.LogWarning($"[GameClient] Poll() - Connection is null! State: {State}");
        }

        private static void ProcessIncomingMessages(HSteamNetConnection conn)
        {
            long t0 = GameClientProfiler.Begin();
            int totalBytes = 0;

            int maxMessagesPerConnectionPoll = Configuration.GetClientProperty<int>("MaxMessagesPerPoll");
            IntPtr[] messages = new IntPtr[maxMessagesPerConnectionPoll];
            int msgCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, messages, maxMessagesPerConnectionPoll);

            //if (msgCount > 0)
            //{
            //	DebugConsole.Log($"[GameClient] ProcessIncomingMessages() - Received {msgCount} messages");
            //}

            for (int i = 0; i < msgCount; i++)
            {
                var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(messages[i]);
                totalBytes += msg.m_cbSize;
                byte[] data = new byte[msg.m_cbSize];
                Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);

                try
                {
                    //DebugConsole.Log($"[GameClient] Processing packet {i+1}/{msgCount}, size: {msg.m_cbSize} bytes, readyToProcess: {PacketHandler.readyToProcess}");
                    PacketHandler.HandleIncoming(data);
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[GameClient] Failed to handle incoming packet: {ex}"); // Prevent crashes from packet handling
                }

                SteamNetworkingMessage_t.Release(messages[i]);
            }
            GameClientProfiler.End(t0, msgCount, totalBytes);
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

            // We've reconnected in game
            MultiplayerSession.InSession = true;
            Game.Instance?.Trigger(MP_HASHES.OnConnected);
            NetworkConfig.RelayClient.OnClientConnected.Invoke();

            var hostId = MultiplayerSession.HostSteamID;
            if (!MultiplayerSession.ConnectedPlayers.ContainsKey(hostId))
            {
                var hostPlayer = new MultiplayerPlayer(hostId);
                MultiplayerSession.ConnectedPlayers[hostId] = hostPlayer;
            }

            // Store the connection handle for host
            MultiplayerSession.ConnectedPlayers[hostId].Connection = Connection;

            DebugConsole.Log("[GameClient] Connection to host established!");

            // Skip mod verification if we are the host
            if (MultiplayerSession.IsHost)
            {
                return;
            }

            PacketHandler.readyToProcess = true;

            if (Utils.IsInGame())
            {
                NetworkConfig.RelayClient.OnContinueConnectionFlow.Invoke();
            }
            else
            {
                NetworkConfig.RelayClient.OnRequestStateOrReturn.Invoke();
            }
        }

        private static void OnDisconnected(string reason, CSteamID remote, ESteamNetworkingConnectionState state)
        {
            DebugConsole.LogWarning($"[GameClient] Connection closed or failed ({state}) for {remote}. Reason: {reason}");

            // If we're intentionally disconnecting for world loading, don't show error or return to title
            // We will reconnect automatically after the world finishes loading via ReconnectFromCache()
            if (GameClient.State == ClientState.LoadingWorld)
            {
                DebugConsole.Log("[GameClient] Ignoring disconnect callback - world is loading, will reconnect after.");
                return;
            }

            switch (state)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    // The host closed our connection
                    if (remote.m_SteamID == MultiplayerSession.HostSteamID)
                    {
                        NetworkConfig.RelayClient.OnReturnToMenu.Invoke();
                    }
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    // Something went wrong locally
                    NetworkConfig.RelayClient.OnReturnToMenu.Invoke();
                    break;
            }
        }

        #region Connection Health
        public static SteamNetConnectionRealTimeStatus_t? QueryConnectionHealth()
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
                    return status;
                }
            }
            return null;
        }

        public static void EvaluateConnectionHealth()
        {
            connectionHealth = QueryConnectionHealth();
        }

        public static SteamNetConnectionRealTimeStatus_t? GetConnectionHealth()
        {
            return connectionHealth;
        }

        public static float GetLocalPacketQuality()
        {
            if (!connectionHealth.HasValue)
                return 0f;

            return connectionHealth.Value.m_flConnectionQualityLocal;
        }

        public static float GetRemotePacketQuality()
        {
            if (!connectionHealth.HasValue)
                return 0f;

            return connectionHealth.Value.m_flConnectionQualityRemote;
        }

        public static int GetPingToHost()
        {
            if (!connectionHealth.HasValue)
                return -1;

            return connectionHealth.Value.m_nPing;
        }

        public static int GetUnackedReliable()
        {
            if (!connectionHealth.HasValue)
                return -1;

            return connectionHealth.Value.m_cbSentUnackedReliable;
        }

        public static int GetPendingUnreliable()
        {
            if (!connectionHealth.HasValue)
                return -1;

            return connectionHealth.Value.m_cbPendingUnreliable;
        }

        public static long GetUsecQueueTime()
        {
            if (!connectionHealth.HasValue)
                return -1;

            return (long)connectionHealth.Value.m_usecQueueTime;
        }
        #endregion
    }
}
