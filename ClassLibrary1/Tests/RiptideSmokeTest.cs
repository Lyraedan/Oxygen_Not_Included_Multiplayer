#if DEBUG
using System;
using ONI_MP.DebugTools;
using Riptide;
using Riptide.Utils;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Social;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using static ONI_MP.STRINGS.UI.MP_OVERLAY;
using System.Net.Sockets;

namespace ONI_MP.Tests
{
    public static class RiptideSmokeTest
    {
        private static Server _server;
        private static Client _client;
        private static bool _packetReceived;

        public static void Run(string ip = "127.0.0.1", ushort port = 7777)
        {
            DebugConsole.Log("[RiptideSmokeTest] Starting");

            try
            {
                RiptideLogger.Initialize(DebugConsole.Log, false);

                _server = new Server("SmokeTest");
                _server.MessageReceived += OnServerMessageReceived;
                _server.Start(port, 1, useMessageHandlers: false);

                //MultiplayerSession.InSession = true; // THIS LINE RIGHT HERE CAUSES THE CRASHING

                //Game.Instance?.Trigger(MP_HASHES.OnConnected);
                //Game.Instance?.Trigger(MP_HASHES.GameServer_OnServerStarted);

                _client = new Client();
                _client.Connected += OnClientConnected;
                _client.Connect($"{ip}:{port}");

                // Tick until packet received or timeout
                int ticks = 0;
                while (!_packetReceived && ticks < 200)
                {
                    _server.Update();
                    _client.Update();
                    ticks++;
                }

                if (!_packetReceived)
                    DebugConsole.LogError("Packet was never received by server", false);

                _client.Disconnect();
                _server.Stop();
                MultiplayerSession.InSession = false;

                DebugConsole.Log("[RiptideSmokeTest] PASSED");
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[RiptideSmokeTest] FAILED: {ex}", false);
            }
        }

        private static void OnClientConnected(object sender, EventArgs e)
        {
            DebugConsole.Log("[RiptideSmokeTest] Client connected");

            TestPacket packet = new TestPacket();
            packet.ClientID = 512;
            SendPacket(packet);
        }

        private static void SendPacket(IPacket packet)
        {
            byte[] bytes = PacketSender.SerializePacketForSending(packet);

            Riptide.Message msg = Riptide.Message.Create(MessageSendMode.Reliable, 1); // dummy ID
            msg.AddBytes(bytes);

            _client.Send(msg);
        }

        private static void OnServerMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ulong clientId = e.FromConnection.Id;
            byte[] rawData = e.Message.GetBytes();
            int size = rawData.Length;

            // Try to read the 4-byte packet type at the start
            int packetType = 0;
            if (rawData.Length >= 4)
                packetType = BitConverter.ToInt32(rawData, 0);

            DebugConsole.Log(
                $"[RiptideSmokeTest] Server received packet from {clientId}, " +
                $"PacketType={packetType}, Size={size} bytes"
            );

            DebugConsole.Log($"[RiptideSmokeTest] Handling packet: " + packetType);

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

            _packetReceived = true;
        }
    }
}
#endif
