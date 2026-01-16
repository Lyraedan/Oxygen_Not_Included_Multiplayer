using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LanServer : RelayServer
    {
        public enum TransportType
        {
            DIRECT,
            STUN // TODO: Research
        }

        private int PORT = 8080;

        private UdpClient udp;
        private bool running;

        private readonly HashSet<IPEndPoint> clients = new HashSet<IPEndPoint>();

        // Anything to init before start
        public override void Prepare()
        {
            PORT = Configuration.Instance.Host.LanSettings.Port;
        }

        public override void Start()
        {
            if (running)
                return;

            udp = new UdpClient(PORT);
            udp.Client.Blocking = false;

            running = true;

            DebugConsole.Log($"[LanServer] LAN UDP Server started on port {PORT}");
        }

        public override void Stop()
        {
            udp?.Close();
            udp = null;

            running = false;
            Debug.Log("LAN UDP Server stopped");
        }

        public override void Update()
        {
            
        }

        public override void CloseConnections()
        {
            clients.Clear();
        }

        public override void OnMessageRecieved()
        {
            if (!running || udp == null)
                return;

            // Poll for incoming packets
            while (udp.Available > 0)
            {
                IPEndPoint remote = null;
                byte[] data = udp.Receive(ref remote);

                if (!clients.Contains(remote))
                {
                    clients.Add(remote);
                    Debug.Log($"Client joined: {remote}");
                }

                HandleMessage(remote, data);
            }
        }

        private void HandleMessage(IPEndPoint sender, byte[] data)
        {
            // Example: relay to all other clients
            foreach (var client in clients)
            {
                if (client.Equals(sender))
                    continue;

                udp.Send(data, data.Length, client);
            }
        }
    }
}
