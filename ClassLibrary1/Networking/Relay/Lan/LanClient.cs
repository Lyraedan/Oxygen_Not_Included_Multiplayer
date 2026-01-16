using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using ONI_MP.Misc;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LanClient : RelayClient
    {
        private int SERVER_PORT = 7777;

        private UdpClient udp;
        private IPEndPoint serverEndpoint;
        private bool connected;

        public string HOST_ADDRESS = "127.0.0.1";

        public static ulong MY_CLIENT_ID = 0;

        public override void Prepare()
        {
            SERVER_PORT = Configuration.Instance.Client.LanSettings.Port;
            HOST_ADDRESS = Configuration.Instance.Client.LanSettings.Ip;
        }

        public override void ConnectToHost()
        {
            if (connected)
                return;

            // HostAddress should come from your RelayClient base
            serverEndpoint = new IPEndPoint(IPAddress.Parse(HOST_ADDRESS), SERVER_PORT);

            udp = new UdpClient();
            udp.Client.Blocking = false;

            // Initial handshake packet (any data works for UDP)
            byte[] hello = Encoding.UTF8.GetBytes("hello");
            udp.Send(hello, hello.Length, serverEndpoint);

            if (udp.Client.LocalEndPoint is IPEndPoint localEndpoint)
            {
                MY_CLIENT_ID = Utils.GetClientId(localEndpoint);
                Debug.Log($"[LanClient] MY_CLIENT_ID = {MY_CLIENT_ID} ({localEndpoint})");
            }
            else
            {
                Debug.LogWarning("[LanClient] Failed to determine local endpoint");
            }

            connected = true;

            Debug.Log($"LAN Client connected to {serverEndpoint}");
        }

        public override void Disconnect()
        {
            connected = false;

            udp?.Close();
            udp = null;

            Debug.Log("[LanClient] LAN Client disconnected");
        }

        public override void ReconnectToSession()
        {
            Disconnect();
            ConnectToHost();
        }

        public override void OnMessageRecieved()
        {
            if (!connected || udp == null)
                return;

            ProcessIncomingMessages();
        }

        public override void Update()
        {
            
        }

        private void ProcessIncomingMessages()
        {
            long t0 = GameClientProfiler.Begin();
            int totalBytes = 0;
            int msgCount = 0;

            int maxMessagesPerPoll = Configuration.GetClientProperty<int>("MaxMessagesPerPoll");

            while (udp.Available > 0 && msgCount < maxMessagesPerPoll)
            {
                IPEndPoint remote = null;
                byte[] data;

                try
                {
                    data = udp.Receive(ref remote);
                }
                catch
                {
                    break;
                }

                // Security: only accept packets from host
                if (!remote.Equals(serverEndpoint))
                    continue;

                msgCount++;
                totalBytes += data.Length;

                try
                {
                    PacketHandler.HandleIncoming(data);
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning(
                        $"[GameClient] Failed to handle incoming packet: {ex}");
                }
            }

            GameClientProfiler.End(t0, msgCount, totalBytes);
        }
    }
}
