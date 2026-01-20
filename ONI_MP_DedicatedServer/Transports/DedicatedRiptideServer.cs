using Riptide;
using Riptide.Utils;
using ONI_MP_DedicatedServer.ONI;

namespace ONI_MP_DedicatedServer.Transports
{
    public class DedicatedRiptideServer : DedicatedTransportServer
    {
        public Server? _server;

        public Dictionary<ulong, ONI.Player> ConnectedPlayers = new Dictionary<ulong, ONI.Player>(); // clientId, Player

        public DedicatedRiptideServer()
        {
            RiptideLogger.Initialize(Console.WriteLine, false);
        }

        public override void Start()
        {
            if (IsRunning())
                return;

            string ip = ServerConfiguration.Instance.Config.Ip;
            int port = ServerConfiguration.Instance.Config.Port;

            _server = new Server("ONI Together: Dedicated Server");
            _server.MessageReceived += OnServerMessageReceived;
            _server.ClientConnected += OnClientConnected;
            _server.ClientDisconnected += OnClientDisconnected;
            _server.Start((ushort)port, 1, useMessageHandlers: false);
            Console.WriteLine($"Started server on {ip}:{port}");
        }

        private void OnClientConnected(object? sender, ServerConnectedEventArgs e)
        {
            ulong clientId = e.Client.Id;
            if(!ConnectedPlayers.ContainsKey(clientId))
            {
                ONI.Player player = new ONI.Player(e.Client, ConnectedPlayers.Count == 0); // If there are no connected clients we are the master
                ConnectedPlayers.Add(clientId, player);
            }
        }

        private void OnClientDisconnected(object? sender, ServerDisconnectedEventArgs e)
        {
            ulong clientId = e.Client.Id;
            bool wasMaster = false;
            if(ConnectedPlayers.TryGetValue(clientId, out ONI.Player? player))
            {
                wasMaster = player.IsMaster;
                ConnectedPlayers.Remove(clientId);
            }

            if (!wasMaster) // We wasn't the master we don't care
                return;

            Console.WriteLine("The master disconnected! Assigning new master!");
            if (_server?.Clients.Length > 0)
            {
                // Find the client with the smallest ping
                Connection? newMasterClient = _server.Clients.OrderBy(c => c.SmoothRTT).FirstOrDefault();

                if (newMasterClient != null && ConnectedPlayers.TryGetValue(newMasterClient.Id, out ONI.Player newMaster))
                {
                    newMaster.UpdateMasterState(true);
                    Console.WriteLine($"New master assigned: Client {newMasterClient.Id} with ping {newMasterClient.SmoothRTT}");
                }
            }
            else
            {
                Console.WriteLine("No other clients connected. No master assigned.");
            }
        }

        private void OnServerMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Recieved message");
        }

        public override void Stop()
        {
            if (!IsRunning())
                return;

            _server.Stop();
        }

        public override bool IsRunning()
        {
            if (_server == null)
                return false;

            return _server.IsRunning;
        }
    }
}
