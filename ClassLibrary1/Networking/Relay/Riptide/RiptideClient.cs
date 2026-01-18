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
        private int SERVER_PORT = 7777;
        private Client client;
        private bool connected;

        public static string HASHED_ADDRESS = string.Empty;
        public string HOST_ADDRESS = "127.0.0.1";

        public static ulong MY_CLIENT_ID = 0;
        public static bool ConnectFromConfig = false;

        public override void Prepare()
        {
            if (ConnectFromConfig)
            {
                SERVER_PORT = Configuration.Instance.Client.LanSettings.Port;
                HOST_ADDRESS = Configuration.Instance.Client.LanSettings.Ip;
            }
            else if (!string.IsNullOrEmpty(HASHED_ADDRESS))
            {
                try
                {
                    LanSettings lan = Utils.DecodeHashedAddress(HASHED_ADDRESS);
                    HOST_ADDRESS = lan.Ip;
                    SERVER_PORT = lan.Port;
                }
                catch
                {
                    DebugConsole.LogError("Failed to decode hashed LAN Address", false);
                }
            }

            // Optional but recommended
            RiptideLogger.Initialize(DebugConsole.Log, true);
        }

        public override void ConnectToHost()
        {
            if (connected)
                return;

            client = new Client();
            client.Connected += OnConnected;
            client.Disconnected += OnDisconnected;
            client.MessageReceived += OnMessageReceived;

            client.Connect($"{HOST_ADDRESS}:{SERVER_PORT}");

            DebugConsole.Log($"[LanClient] Connecting to {HOST_ADDRESS}:{SERVER_PORT}");
        }

        public override void Disconnect()
        {
            connected = false;

            if (client != null)
            {
                client.Disconnect();
                client = null;
            }

            DebugConsole.Log("[LanClient] LAN Client disconnected");
        }

        public override void ReconnectToSession()
        {
            Disconnect();
            ConnectToHost();
        }

        public override void OnMessageRecieved()
        {
            // Riptide uses MessageReceived event
        }

        public override void Update()
        {
            client?.Update();
        }

        private void OnConnected(object sender, EventArgs e)
        {
            connected = true;

            // Session-scoped ID assigned by server
            MY_CLIENT_ID = client.Id;

            DebugConsole.Log($"[LanClient] Connected to server with MY_CLIENT_ID = {MY_CLIENT_ID}");
        }

        private void OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            connected = false;

            DebugConsole.Log($"[LanClient] Disconnected from server ({e.Reason})");
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Riptide.Message message = e.Message;
            int totalBytes = message.BytesInUse;
            int msgCount = 1;

            long t0 = GameClientProfiler.Begin();

            byte[] data = message.GetBytes();

            try
            {
                PacketHandler.HandleIncoming(data);
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[LanClient] Failed to handle incoming packet: {ex}");
            }

            GameClientProfiler.End(t0, msgCount, totalBytes);
        }
    }
}
