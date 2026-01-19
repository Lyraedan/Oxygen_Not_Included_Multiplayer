using System;
using System.Net;
using Riptide;
using Riptide.Utils;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using UnityEngine.Sprites;
using static LogicPorts;

namespace ONI_MP.Networking.Relay.Lan
{
    public class RiptideServer : RelayServer
    {
        private static Server _server;
        private static Client _client; // Server client (Other users will use GameClient)

        public static Client Client
        {
            get { return _client; }
        }

        public static ulong ClientID
        {
            get
            {
                if (_client == null || _client.IsNotConnected)
                    return Utils.NilUlong();
                else
                    return _client.Id;
            }
        }

        public override void Prepare()
        {
            RiptideLogger.Initialize(DebugConsole.Log, false);
        }

        public override void Start()
        {
            if (_server != null)
                return;

            string ip = Configuration.Instance.Host.LanSettings.Ip;
            int port = Configuration.Instance.Host.LanSettings.Port;
            int maxClients = Configuration.Instance.Host.MaxLobbySize;

            _server = new Server("Lan/Riptide");
            _server.MessageReceived += OnServerMessageReceived;
            _server.ClientConnected += ServerOnClientConnected;
            _server.ClientDisconnected += ServerOnClientDisconnected;
            _server.Start((ushort)port, (ushort)maxClients, useMessageHandlers: false);
            DebugConsole.Log("[RiptideServer] Riptide server started!");

            _client = new Client("Lan/Riptide/HostClient");
            _client.Connected += OnLocalClientConnected;
            _client.Disconnected += OnLocalClientDisconnected;
            _client.Connect($"127.0.0.1:{port}", useMessageHandlers: false); // Since we're running locally we should be able to connect this way
        }

        private void OnLocalClientConnected(object sender, EventArgs e)
        {
            DebugConsole.Log("[RiptideServer] Host client connected to server!");
            MultiplayerSession.SetHost(GetClientID());
            MultiplayerSession.InSession = true;
        }

        private void OnLocalClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            DebugConsole.Log("[RiptideServer] Host client disconnected from server!");
            MultiplayerSession.HostUserID = Utils.NilUlong();
            MultiplayerSession.InSession = false;
        }

        private void ServerOnClientConnected(object sender, ServerConnectedEventArgs e)
        {
            ulong clientId = e.Client.Id;
            MultiplayerPlayer player;
            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out player))
            {
                player = new MultiplayerPlayer(clientId);
                MultiplayerSession.ConnectedPlayers.Add(clientId, player);
            }
            player.Connection = e.Client;
            DebugConsole.Log($"New client connected: {clientId}");
        }

        private void ServerOnClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            ulong clientId = e.Client.Id;
            if (MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out MultiplayerPlayer player))
            {
                player.Connection = null;

                MultiplayerSession.ConnectedPlayers.Remove(clientId);

                DebugConsole.Log($"Player {clientId} disconnected.");
            }
            else
            {
                DebugConsole.LogWarning($"Disconnected client {clientId} was not found in ConnectedPlayers.");
            }
            ReadyManager.RefreshReadyState();
        }

        private void OnServerMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ulong clientId = e.FromConnection.Id;
            byte[] rawData = e.Message.GetBytes();
            int size = rawData.Length;

            int packetType = 0;
            if (rawData.Length >= 4)
                packetType = BitConverter.ToInt32(rawData, 0);

            DebugConsole.Log(
                $"[RiptideSmokeTest] Server received packet from {clientId}, " +
                $"PacketType={packetType}, Size={size} bytes"
            );

            long t0 = GameServerProfiler.Begin();

            try
            {
                PacketHandler.HandleIncoming(rawData);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanServer] Failed to handle packet {packetType}: {ex}");
            }

            GameServerProfiler.End(t0, 1, size);
        }

        public override void Stop()
        {
            if (_server == null)
                return;

            if (!_server.IsRunning)
                return;

            if (!_client.IsNotConnected)
            {
                _client.Disconnect();
                _client = null;
            }

            _server.Stop();
            _server = null;
        }

        // The server is shutting down so disconnect everyone
        public override void CloseConnections()
        {
            if (_server == null || !_server.IsRunning)
                return;

            // Disconnect all clients
            foreach (Connection client in _server.Clients)
            {
                if (!client.IsNotConnected)
                {
                    DebugConsole.Log($"Client {client.Id} disconnected by server shutdown.");
                    _server.DisconnectClient(client);
                }
            }

            // Clear our session player list
            MultiplayerSession.ConnectedPlayers.Clear();
        }

        public override void OnMessageRecieved()
        {
            // Riptide uses its own OnServerMessageReceived function
        }

        public override void Update()
        {
            _server?.Update();
            _client?.Update();
        }

        public ulong GetClientID()
        {
            if (_client == null || _client.IsNotConnected)
                return Utils.NilUlong();

            return _client.Id;
        }
    }
}
