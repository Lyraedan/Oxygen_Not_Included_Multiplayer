using System;
using Riptide;
using Riptide.Utils;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using ONI_MP.Networking.Transfer;
using System.Collections.Generic;
using ONI_MP.UI;
using static ResearchTypes;

namespace ONI_MP.Networking.Transport.Lan
{
    public class RiptideServer : TransportServer
    {
        private static Server _server;
        private static Client _client; // Server client (Other users will use GameClient)
        private TcpFileTransferServer _tcpTransfer;

        public TcpFileTransferServer TcpTransfer => _tcpTransfer;

        public static Server ServerInstance
        {
            get { return _server; }
        }

        public static Client Client
        {
            get { return _client; }
        }

        public List<ulong> ClientList { get; internal set; } = new();

        public static ulong CLIENT_ID { get; private set; }

        public override void Prepare()
        {
            RiptideLogger.Initialize(DebugConsole.Log, false);
        }

        public override void Start()
        {
            if (_server != null)
                return;

            ChatScreen.PendingMessage pending = ChatScreen.GeneratePendingMessage(string.Format(STRINGS.UI.MP_CHATWINDOW.CHAT_SERVER_STARTED, $"LAN"));
            ChatScreen.QueueMessage(pending);

            string ip = Configuration.Instance.Host.LanSettings.Ip;
            int port = Configuration.Instance.Host.LanSettings.Port;
            int maxClients = Configuration.Instance.Host.MaxLobbySize;

            _server = new Server("Lan/Riptide");
            _server.MessageReceived += OnServerMessageReceived;
            _server.ConnectionFailed += OnClientConnectionFailed;
            _server.ClientConnected += ServerOnClientConnected;
            _server.ClientDisconnected += ServerOnClientDisconnected;
            _server.Start((ushort)port, (ushort)maxClients, useMessageHandlers: false);
            DebugConsole.Log("[RiptideServer] Riptide server started!");

            _tcpTransfer = new TcpFileTransferServer();
            _tcpTransfer.Start(port);

            _client = new Client("Lan/Riptide/HostClient");
            _client.Connected += OnLocalClientConnected;
            _client.Disconnected += OnLocalClientDisconnected;
            DebugConsole.Log("[RiptideServer] Connecting host client!");
            _client.Connect($"{ip}:{port}", useMessageHandlers: false);
        }

        private void OnClientConnectionFailed(object sender, ServerConnectionFailedEventArgs e)
        {
            int id = e.Client.Id;
            DebugConsole.Log("[RiptideServer] A client failed to connect to the server.");
            ChatScreen.PendingMessage pending = ChatScreen.GeneratePendingMessage(string.Format(STRINGS.UI.MP_CHATWINDOW.CHAT_CLIENT_FAILED, $"Player {id}"));
            ChatScreen.QueueMessage(pending);
        }

        private void OnLocalClientConnected(object sender, EventArgs e)
        {
            CLIENT_ID = _client.Id;
            //AddClientToList(CLIENT_ID);
            DebugConsole.Log("[RiptideServer] Host client connected to server!");
            MultiplayerSession.SetHost(GetClientID());
            MultiplayerSession.InSession = true;
        }

        private void OnLocalClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            CLIENT_ID = Utils.NilUlong();
            //RemoveClientFromList(CLIENT_ID);
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

            AddClientToList(e.Client.Id);
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

            RemoveClientFromList(clientId);
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
                $"[Riptide] Server received packet from {clientId}, " +
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

            ChatScreen.PendingMessage pending = ChatScreen.GeneratePendingMessage(string.Format(STRINGS.UI.MP_CHATWINDOW.CHAT_SERVER_STOPPED, $"LAN"));
            ChatScreen.QueueMessage(pending);

            if (!_client.IsNotConnected)
            {
                _client.Disconnect();
                _client = null;
            }

            _tcpTransfer?.Stop();
            _tcpTransfer = null;

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

        public void AddClientToList(ulong id)
        {
            if (ClientList.Contains(id))
                return;

            ClientList.Add(id);

            ChatScreen.PendingMessage pending = ChatScreen.GeneratePendingMessage(string.Format(STRINGS.UI.MP_CHATWINDOW.CHAT_CLIENT_JOINED, $"Player {id}"));
            ChatScreen.QueueMessage(pending);
            Game.Instance?.Trigger(MP_HASHES.OnPlayerJoined);
        }

        public void RemoveClientFromList(ulong id)
        {
            if (!ClientList.Contains(id))
                return;

            ClientList.Remove(id);

            ChatScreen.PendingMessage pending = ChatScreen.GeneratePendingMessage(string.Format(STRINGS.UI.MP_CHATWINDOW.CHAT_CLIENT_LEFT, $"Player {id}"));
            ChatScreen.QueueMessage(pending);
            Game.Instance?.Trigger(MP_HASHES.OnPlayerLeft);
        }
        public ulong GetClientID()
        {
            if (_client == null || _client.IsNotConnected)
                return Utils.NilUlong();

            return _client.Id;
        }
    }
}
