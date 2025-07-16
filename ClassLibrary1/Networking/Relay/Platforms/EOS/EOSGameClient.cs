using System;
using UnityEngine;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.Relay.Platforms.EOS;
using ONI_MP.Networking.States;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;

namespace ONI_MP.Networking.Platforms.EOS
{
    public class EosGameClient : IGameClient
    {
        private ClientState _state = ClientState.Disconnected;
        private ProductUserId _hostId;
        private ProductUserId _cachedHostId;

        private bool _isHardSyncInProgress = false;

        public ClientState State => _state;
        public bool IsConnected => _state == ClientState.Connected || _state == ClientState.InGame;

        public bool IsHardSyncInProgress
        {
            get => _isHardSyncInProgress;
            set => _isHardSyncInProgress = value;
        }

        public void Init()
        {
            DebugConsole.Log("[EosGameClient] Init() called.");
        }

        public void ConnectToHost(string id, bool showLoadingScreen = true)
        {
            try
            {
                _hostId = ProductUserId.FromString(id);
            }
            catch (Exception)
            {
                DebugConsole.LogError($"[EosGameClient] Invalid ProductUserId: {id}");
                return;
            }

            if (showLoadingScreen)
            {
                string host_name = PacketSender.Platform.GetPlayerName(id);
                MultiplayerOverlay.Show($"Connecting to {host_name}...");
            }

            DebugConsole.Log($"[EosGameClient] Connecting to host: {_hostId}");

            var options = new AcceptConnectionOptions
            {
                LocalUserId = EOSPlatform.LocalUserId,
                RemoteUserId = _hostId,
                SocketId = new SocketId { SocketName = "ONI_MP" }
            };

            EOSPlatform.P2P.AcceptConnection(options);
            SetState(ClientState.Connecting);
        }

        public void ReconnectToSession()
        {
            Disconnect();
            System.Threading.Thread.Sleep(100);

            if (_cachedHostId != null && _cachedHostId.IsValid())
            {
                DebugConsole.Log("[EosGameClient] Reconnecting to session.");
                ConnectToHost(_cachedHostId.ToString());
            }
            else
            {
                DebugConsole.LogWarning("[EosGameClient] Cached host ID is invalid; cannot reconnect.");
            }
        }

        public void Disconnect()
        {
            if (_hostId != null && _hostId.IsValid())
            {
                var options = new CloseConnectionOptions
                {
                    LocalUserId = EOSPlatform.LocalUserId,
                    RemoteUserId = _hostId,
                    SocketId = new SocketId { SocketName = "ONI_MP" }
                };

                EOSPlatform.P2P.CloseConnection(options);
                _hostId = null;
            }

            SetState(ClientState.Disconnected);
            MultiplayerSession.InSession = false;
        }

        public void Poll()
        {
            if (IsConnected)
                ReceiveMessages();
        }

        public INetworkConnection GetHostConnection()
        {
            return _hostId != null ? new EosConnection(_hostId) : null;
        }

        public int? GetPingToHost()
        {
            // EOS SDK does not support ping queries directly in P2P. Return null.
            return null;
        }

        public void CacheCurrentServer()
        {
            _cachedHostId = _hostId;
            DebugConsole.Log($"[EosGameClient] Cached host: {_cachedHostId}");
        }

        public void ReconnectFromCache()
        {
            if (_cachedHostId != null && _cachedHostId.IsValid())
            {
                DebugConsole.Log($"[EosGameClient] Reconnecting from cache: {_cachedHostId}");
                ConnectToHost(_cachedHostId.ToString());
            }
            else
            {
                DebugConsole.LogWarning("[EosGameClient] No cached host to reconnect to.");
            }
        }

        public void SetState(ClientState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                DebugConsole.Log($"[EosGameClient] State changed to: {_state}");
            }
        }

        private void ReceiveMessages()
        {
            int max = Configuration.GetClientProperty<int>("MaxMessagesPerPoll");

            for (int i = 0; i < max; i++)
            {
                var receiveOptions = new ReceivePacketOptions
                {
                    LocalUserId = EOSPlatform.LocalUserId,
                    MaxDataSizeBytes = 1024,
                    RequestedChannel = 0,
                };

                var buffer = new byte[receiveOptions.MaxDataSizeBytes];
                var result = EOSPlatform.P2P.ReceivePacket(
                    receiveOptions,
                    out var remoteUserId,
                    out var socketId,
                    out byte channel,
                    ref buffer,
                    out var bytesWritten);

                if (result == Epic.OnlineServices.Result.Success && bytesWritten > 0)
                {
                    var actualData = new byte[bytesWritten];
                    Array.Copy(buffer, actualData, bytesWritten);

                    try
                    {
                        PacketHandler.HandleIncoming(actualData);
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogError($"[EosGameClient] Failed to handle packet: {ex}");
                    }

                    SetState(ClientState.Connected); // Transition after first successful packet
                }
                else
                {
                    break;
                }
            }
        }
    }
}
