using System;
using System.Net;
using Riptide;
using Riptide.Utils;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using ONI_MP.Misc;

namespace ONI_MP.Networking.Relay.Lan
{
    public class RiptideClient : RelayClient
    {
        private static Client _client;

        public static Client Client
        {
            get { return _client; }
        }

        public override void Prepare()
        {
            RiptideLogger.Initialize(DebugConsole.Log, false);
        }

        public override void ConnectToHost()
        {
            if (!_client.IsNotConnected)
                return;

            string ip = Configuration.Instance.Client.LanSettings.Ip;
            int port = Configuration.Instance.Client.LanSettings.Port;
            _client = new Client();
            _client.Connected += OnConnectedToServer;
            _client.Disconnected += OnDisconnectedFromServer;
            _client.MessageReceived += OnMessageRecievedFromServer;
            _client.Connect($"{ip}:{port}");
        }

        private void OnMessageRecievedFromServer(object sender, MessageReceivedEventArgs e)
        {
            ulong clientId = e.FromConnection.Id;
            byte[] rawData = e.Message.GetBytes();
            int size = rawData.Length;

            // Try to read the 4-byte packet type at the start
            int packetType = 0;
            if (rawData.Length >= 4)
                packetType = BitConverter.ToInt32(rawData, 0);

            //DebugConsole.Log(
            //    $"[RiptideSmokeTest] Server received packet from {clientId}, " +
            //    $"PacketType={packetType}, Size={size} bytes"
            //);

            //DebugConsole.Log($"[RiptideSmokeTest] Handling packet: " + packetType);

            long t0 = GameServerProfiler.Begin();

            try
            {
                // Pass the full payload (including packetType) to your handler
                PacketHandler.HandleIncoming(rawData);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanServer] Failed to handle packet {packetType}: {ex}");
            }

            GameServerProfiler.End(t0, 1, size);
        }

        private void OnConnectedToServer(object sender, EventArgs e)
        {
            OnClientConnected.Invoke();
            MultiplayerSession.HostUserID = 1; // Host's client is always 1
        }
        private void OnDisconnectedFromServer(object sender, DisconnectedEventArgs e)
        {
            OnClientDisconnected.Invoke();
            MultiplayerSession.HostUserID = Utils.NilUlong();
        }

        public override void Disconnect()
        {
            if (_client.IsNotConnected)
                return;

            _client.Disconnect();
        }

        public override void OnMessageRecieved()
        {
            // Riptide uses OnMessageRecievedFromServer
        }

        public override void ReconnectToSession()
        {

        }

        public override void Update()
        {
            _client?.Update();
        }

        public ulong GetClientID()
        {
            return Utils.NilUlong();
        }
    }
}
