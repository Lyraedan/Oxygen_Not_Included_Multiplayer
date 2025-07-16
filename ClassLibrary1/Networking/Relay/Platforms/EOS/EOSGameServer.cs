using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.States;
using ONI_MP.Networking.Relay.Platforms.EOS;

namespace ONI_MP.Networking.Relay.Platforms.EOS
{
    public class EosGameServer : IGameServer
    {
        private ServerState _state = ServerState.Stopped;
        public ServerState State => _state;

        private void SetState(ServerState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                DebugConsole.Log($"[EosGameServer] State changed to: {_state}");
            }
        }

        public void Start()
        {
            SetState(ServerState.Preparing);

            if (EOSPlatform.LocalUserId == null || !EOSPlatform.LocalUserId.IsValid())
            {
                SetState(ServerState.Error);
                DebugConsole.LogError("[EosGameServer] EOS LocalUserId is not valid!");
                return;
            }

            SetState(ServerState.Starting);

            var connRequestOptions = new AddNotifyPeerConnectionRequestOptions();
            EOSPlatform.P2P.AddNotifyPeerConnectionRequest(connRequestOptions, null, OnPeerConnectionRequested);

            var connClosedOptions = new AddNotifyPeerConnectionClosedOptions();
            EOSPlatform.P2P.AddNotifyPeerConnectionClosed(connClosedOptions, null, OnPeerDisconnected);


            MultiplayerSession.InSession = true;
            SetState(ServerState.Started);

            DebugConsole.Log("[EosGameServer] Server started and listening for peers.");
        }

        public void Shutdown()
        {
            SetState(ServerState.Stopped);

            foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
            {
                if (player.Connection is EosConnection eosConn)
                {
                    var closeOptions = new CloseConnectionOptions
                    {
                        LocalUserId = EOSPlatform.LocalUserId,
                        RemoteUserId = eosConn.RemoteUserId,
                        SocketId = new SocketId { SocketName = "ONI_MP" }
                    };

                    EOSPlatform.P2P.CloseConnection(closeOptions);
                    player.Connection = null;
                }
            }

            MultiplayerSession.InSession = false;
            DebugConsole.Log("[EosGameServer] Shutdown complete.");
        }

        public void Update()
        {
            if (State != ServerState.Started)
                return;

            ReceiveMessages();
        }

        private void OnPeerConnectionRequested(OnIncomingConnectionRequestInfo info)
        {
            DebugConsole.Log($"[EosGameServer] Incoming connection from {info.RemoteUserId}");

            var acceptOptions = new AcceptConnectionOptions
            {
                LocalUserId = EOSPlatform.LocalUserId,
                RemoteUserId = info.RemoteUserId,
                SocketId = info.SocketId
            };

            var result = EOSPlatform.P2P.AcceptConnection(acceptOptions);
            if (result != Epic.OnlineServices.Result.Success)
            {
                DebugConsole.LogError($"[EosGameServer] Failed to accept connection: {result}");
                return;
            }
            OnConnectionAccepted(info);
        }

        void OnConnectionAccepted(OnIncomingConnectionRequestInfo info)
        {
            // Create new connection wrapper
            var newConnection = new EosConnection(info.RemoteUserId);

            // Add or update player in MultiplayerSession
            string playerId = newConnection.Id;
            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(playerId, out var player))
            {
                player = new MultiplayerPlayer(playerId);
                MultiplayerSession.ConnectedPlayers[playerId] = player;
            }
            player.Connection = newConnection;
        }

        private void OnPeerDisconnected(OnRemoteConnectionClosedInfo info)
        {
            var id = info.RemoteUserId.ToString();
            if (MultiplayerSession.ConnectedPlayers.TryGetValue(id, out var player))
            {
                player.Connection = null;
            }

            MultiplayerSession.ConnectedPlayers.Remove(id);

            DebugConsole.Log($"[EosGameServer] Disconnected from {info.RemoteUserId}");
        }

        private void ReceiveMessages()
        {
            int max = Configuration.GetHostProperty<int>("MaxMessagesPerPoll");

            //SocketId socketId = new SocketId { SocketName = "ONI_MP" };

            for (int i = 0; i < max; i++)
            {

                var receiveOptions = new ReceivePacketOptions
                {
                    LocalUserId = EOSPlatform.LocalUserId,
                    MaxDataSizeBytes = 1024, // 1MB
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
                    PacketHandler.HandleIncoming(actualData);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
