using System;
using System.Net;
using Riptide;
using Riptide.Utils;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using UnityEngine.Sprites;

namespace ONI_MP.Networking.Relay.Lan
{
    public class RiptideServer : RelayServer
    {
        private Server server;
        private int port;

        public static ulong MY_CLIENT_ID = 0;

        public override void Prepare()
        {
            // Optional but recommended
            RiptideLogger.Initialize(DebugConsole.Log, false);
        }

        public override void Start()
        {
            if (server != null)
                return;

            string ip = Configuration.Instance.Host.LanSettings.Ip;
            port = Configuration.Instance.Host.LanSettings.Port;

            server = new Server("Riptide LAN Server");
            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;
            server.MessageReceived += OnMessageReceived;

            server.Start((ushort)port, (ushort)Configuration.Instance.Host.MaxLobbySize);
            MY_CLIENT_ID = 0;

            DebugConsole.Log($"[LanServer] Riptide LAN server started on {ip}:{port}");
        }

        public override void Stop()
        {
            if (server == null)
                return;

            server.Stop();
            server = null;

            DebugConsole.Log("[LanServer] Riptide LAN server stopped");
        }

        public override void Update()
        {
            server?.Update();
        }

        public override void CloseConnections()
        {
            foreach (MultiplayerPlayer player in MultiplayerSession.AllPlayers)
                player.Connection = null;

            MultiplayerSession.ConnectedPlayers.Clear();
        }

        public override void OnMessageRecieved()
        {
            // Riptide uses MessageReceived event
        }

        private void OnClientConnected(object sender, ServerConnectedEventArgs e)
        {
            ulong clientId = e.Client.Id;

            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out var player))
            {
                player = new MultiplayerPlayer(clientId);
                MultiplayerSession.ConnectedPlayers.Add(clientId, player);
            }

            player.Connection = e.Client;

            DebugConsole.Log($"[LanServer] Client connected: {clientId}");
        }

        private void OnClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            ulong clientId = e.Client.Id;

            if (MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out var player))
                player.Connection = null;

            MultiplayerSession.ConnectedPlayers.Remove(clientId);

            DebugConsole.Log($"[LanServer] Client disconnected: {clientId} ({e.Reason})");
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Riptide.Message message = e.Message;
            int size = message.BytesInUse;

            byte[] data = message.GetBytes();

            ulong clientId = e.FromConnection.Id;
            long t0 = GameServerProfiler.Begin();

            try
            {
                PacketHandler.HandleIncoming(data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanServer] Failed to handle packet from {clientId}: {ex}");
            }

            GameServerProfiler.End(t0, 1, size);
        }
    }
}
