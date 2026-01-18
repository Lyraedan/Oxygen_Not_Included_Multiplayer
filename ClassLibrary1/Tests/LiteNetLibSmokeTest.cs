#if DEBUG
using System;
using System.Net;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Social;
using ONI_MP.Networking.Profiling;

namespace ONI_MP.Tests
{
    public static class LiteNetLibSmokeTest
    {
        private static EventBasedNetListener _listener;
        private static NetManager _server;
        private static NetManager _client;
        private static NetPeer _clientPeer;
        private static bool _packetReceived;

        public static void Run(int port = 7777)
        {
            DebugConsole.Log("[LiteNetLibSmokeTest] Starting");

            try
            {
                _packetReceived = false;

                // Setup server listener
                _listener = new EventBasedNetListener();
                _listener.NetworkReceiveEvent += OnNetworkReceive;

                // Start server
                _server = new NetManager(_listener) { AutoRecycle = true };
                _server.Start(port);
                DebugConsole.Log($"[LiteNetLibSmokeTest] Server started on port {port}");

                // Start client
                _client = new NetManager(new EventBasedNetListener());
                _client.Start();

                _clientPeer = _client.Connect("127.0.0.1", port, "LiteNetLibTest");

                int ticks = 0;
                while (!_packetReceived && ticks < 200)
                {
                    _server.PollEvents();
                    _client.PollEvents();

                    // Send packet once client is connected
                    if (_clientPeer != null && _clientPeer.ConnectionState == ConnectionState.Connected && ticks == 0)
                    {
                        ChatMessagePacket packet = new ChatMessagePacket("Hello from LiteNetLib smoke test");
                        SendPacket(_clientPeer, packet);
                    }

                    Thread.Sleep(10);
                    ticks++;
                }

                if (!_packetReceived)
                    throw new Exception("Packet was never received by server");

                _client.DisconnectAll();
                _server.Stop();

                DebugConsole.Log("[LiteNetLibSmokeTest] PASSED");
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[LiteNetLibSmokeTest] FAILED: {ex}", false);
            }
        }

        private static void SendPacket(NetPeer peer, IPacket packet)
        {
            byte[] bytes = PacketSender.SerializePacketForSending(packet);
            peer.Send(bytes, DeliveryMethod.ReliableOrdered);
        }

        private static void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            int size = reader.AvailableBytes;
            byte[] data = reader.GetRemainingBytes();

            ulong clientId = Utils.GetClientId(peer);
            long t0 = GameServerProfiler.Begin();

            try
            {
                PacketHandler.HandleIncoming(data);
                DebugConsole.Log("[LiteNetLibSmokeTest] Handling packet!");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[LiteNetLibSmokeTest] Failed to handle packet from {clientId}: {ex}");
            }

            GameServerProfiler.End(t0, 1, size);
            reader.Recycle();

            _packetReceived = true; // Mark packet received for the loop
        }
    }
}
#endif
