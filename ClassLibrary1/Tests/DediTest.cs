using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking;
using Riptide;
using Riptide.Utils;

namespace ONI_MP.Tests
{
    public class DediTest
    {
        private static Client _client;

        public static void Connect(string ip = "127.0.0.1", int port = 7777)
        {
            RiptideLogger.Initialize(DebugConsole.Log, false);
            _client = new Client("Dedicated client test");
            _client.Connected += OnClientConnected;
            _client.Disconnected += OnClientDisconnected;

            DebugConsole.Log($"Connecting to {ip}:{port}");
            _client.Connect($"{ip}:{port}", useMessageHandlers: false);
        }

        private static void OnClientConnected(object sender, EventArgs e)
        {
            DebugConsole.Log("[DediTest] Successfully connected to the Dedicated server!");

            SendTestPacket();
        }

        private static void OnClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            DebugConsole.Log("[DediTest] Successfully disconnected to the Dedicated server!");
        }

        public static void Disconnect()
        {
            if (_client == null || _client.IsNotConnected)
                return;
            _client.Disconnect();
        }

        public static void SendTestPacket()
        {
            TestPacket testPacket = new TestPacket();
            testPacket.ClientID = 123;
            SendPacket(testPacket);
            DebugConsole.Log("[DediTest] Sent test packet!");
        }

        private static void SendPacket(IPacket packet)
        {
            byte[] bytes = PacketSender.SerializePacketForSending(packet);

            Riptide.Message msg = Riptide.Message.Create(MessageSendMode.Reliable, 1); // dummy ID
            msg.AddBytes(bytes);

            _client.Send(msg);
        }
    }
}
