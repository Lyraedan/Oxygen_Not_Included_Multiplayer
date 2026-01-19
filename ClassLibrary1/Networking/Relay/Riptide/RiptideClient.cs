using System;
using System.Net;
using Riptide;
using Riptide.Utils;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using ONI_MP.Misc;
using System.Collections.Concurrent;

namespace ONI_MP.Networking.Relay.Lan
{
    public class RiptideClient : RelayClient
    {
        private static Client _client;

        public static Client Client
        {
            get { return _client; }
        }

        private static readonly ConcurrentQueue<byte[]> _incomingPackets = new ConcurrentQueue<byte[]>();

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
            _client = new Client("RiptideClient");
            _client.Connected += OnConnectedToServer;
            _client.Disconnected += OnDisconnectedFromServer;
            _client.MessageReceived += OnMessageRecievedFromServer;
            _client.Connect($"{ip}:{port}", useMessageHandlers: false);
        }

        private void OnMessageRecievedFromServer(object sender, MessageReceivedEventArgs e)
        {
            byte[] rawData = e.Message.GetBytes();
            _incomingPackets.Enqueue(rawData);
        }

        private void OnConnectedToServer(object sender, EventArgs e)
        {
            OnClientConnected.Invoke();
            MultiplayerSession.SetHost(1); // Host's client is always 1
            MultiplayerSession.InSession = true;
        }
        private void OnDisconnectedFromServer(object sender, DisconnectedEventArgs e)
        {
            OnClientDisconnected.Invoke();
            MultiplayerSession.HostUserID = Utils.NilUlong();
            MultiplayerSession.InSession = false;
        }

        public override void Disconnect()
        {
            if (_client.IsNotConnected)
                return;

            _client.Disconnect();
        }

        public override void OnMessageRecieved()
        {
            while (_incomingPackets.TryDequeue(out var rawData))
            {
                int size = rawData.Length;

                int packetType = rawData.Length >= 4
                    ? BitConverter.ToInt32(rawData, 0)
                    : 0;

                long t0 = GameServerProfiler.Begin();

                try
                {
                    PacketHandler.HandleIncoming(rawData);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LanClient] Failed to handle packet {packetType}: {ex}");
                }

                GameServerProfiler.End(t0, 1, size);
            }
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
            if (_client == null || _client.IsNotConnected)
                return Utils.NilUlong();

            return _client.Id;
        }
    }
}
