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
            int maxPlayers = ServerConfiguration.Instance.Config.MaxLobbySize;

            _server = new Server("ONI Together: Dedicated Server");
            _server.MessageReceived += OnServerMessageReceived;
            _server.ClientConnected += OnClientConnected;
            _server.ClientDisconnected += OnClientDisconnected;
            _server.Start((ushort)port, (ushort)maxPlayers, useMessageHandlers: false);
            Console.WriteLine($"Started server on {ip}:{port}");
        }

        private void OnClientConnected(object? sender, ServerConnectedEventArgs e)
        {
            ulong clientId = e.Client.Id;
            if(!ConnectedPlayers.ContainsKey(clientId))
            {
                ONI.Player player = new ONI.Player(e.Client, ConnectedPlayers.Count == 0); // If there are no connected clients we are the master
                ConnectedPlayers.Add(clientId, player);
                Console.Write($"A new player joined the server. {player.ClientID} : {player.IsMaster}");
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
            Console.Write($"A player disconnected from the server. {clientId} : {wasMaster}");

            if (!wasMaster) // We wasn't the master we don't care
                return;

            Console.WriteLine("\nThe master disconnected! Attempting to assign a new master!");
            if (_server?.Clients.Length > 0)
            {
                // Find the client with the smallest ping
                Connection? newMasterClient = _server.Clients.Where(c => c.SmoothRTT >= 0).OrderBy(c => c.SmoothRTT).FirstOrDefault();

                if (newMasterClient != null && ConnectedPlayers.TryGetValue(newMasterClient.Id, out ONI.Player newMaster))
                {
                    newMaster.UpdateMasterState(true);
                    Console.WriteLine($"New master assigned: Client {newMasterClient.Id} with ping {newMasterClient.SmoothRTT}");

                    // Notify this client that they are now the master, TODO: Send a migration packet
                }
            }
            else
            {
                Console.WriteLine("No other clients connected. No master assigned.");
            }
        }

        private void OnServerMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ulong clientId = e.FromConnection.Id;
            byte[] rawData = e.Message.GetBytes();
            int size = rawData.Length;

            if (ConnectedPlayers.TryGetValue(clientId, out ONI.Player player))
            {
                int packetType = 0;
                if (rawData.Length >= 4)
                    packetType = BitConverter.ToInt32(rawData, 0);

                Console.WriteLine(
                    $"\nServer received packet from {clientId}, " +
                    $"PacketType={packetType}, Size={size} bytes"
                );

                MessageSendMode SendMode = MessageSendMode.Reliable;
                // Wrap this as a DedicatedServerMessagePacket
                byte[] relayedPacketData = Utils.SerializePacketForSending(Utils.DEDICATED_SERVER_PACKET_ID, (writer) =>
                {
                    writer.Write(packetType); // PacketID
                    writer.Write((int)SendMode); // Send Type
                    writer.Write(rawData.Length);
                    writer.Write(rawData); // PacketData
                });

                Riptide.Message msg = Riptide.Message.Create(SendMode, 1);

                // Check if player.IsMaster
                // If we're not the master, send this to the master
                // If we're the master, send it to everyone else and not the master
                if (player.IsMaster)
                {
                    //_server.SendToAll(msg);
                    var slaves = ConnectedPlayers.Values.Where(p => !p.IsMaster);
                    foreach(ONI.Player client in slaves)
                    {
                        client.Connection.Send(msg);
                    }
                } else
                {
                    ONI.Player master = ConnectedPlayers.Values.Where(p => p.IsMaster).FirstOrDefault();
                    if (master != null)
                    {
                        _server.Send(msg, master.Connection);
                    }
            }
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

        public override void Update()
        {
            _server?.Update();
        }

        public override Dictionary<ulong, ONI.Player> GetPlayers()
        {
            return ConnectedPlayers;
        }
    }
}
