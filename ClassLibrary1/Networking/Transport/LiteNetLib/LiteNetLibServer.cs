using System;
using System.Net;
using Mono.Nat;
using LiteNetLib.Utils;
using LiteNetLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Profiling;
using System.Collections.Concurrent;

namespace ONI_MP.Networking.Transport.Lan
{
    public class LiteNetLibServer : TransportServer, INetEventListener
    {
        public enum TransportType
        {
            DIRECT,
            NAT,
            //STUN //TODO Research
        }

        public TransportType transport = TransportType.DIRECT;
        private NetManager netManager;
        private NetDataWriter writer;

        private int port;
        public static ulong MY_CLIENT_ID = 0;

        private readonly ConcurrentQueue<(NetPeer peer, byte[] data)> incomingPackets = new();

        public override void Prepare()
        {
            Profiler.Scope();

            writer = new NetDataWriter();
        }

        public override void Start()
        {
            Profiler.Scope();

            if (netManager != null)
                return;

            string ip = Configuration.Instance.Host.LanSettings.Ip;
            port = Configuration.Instance.Host.LanSettings.Port;

            netManager = new NetManager(this)
            {
                IPv6Enabled = false,
                UnconnectedMessagesEnabled = false,
                AutoRecycle = false,
            };

            netManager.Start(port);

            if(transport.Equals(TransportType.NAT))
            {
                // NAT Discovery
                try
                {
                    NatUtility.DeviceFound += OnNatDeviceFound;
                    NatUtility.StartDiscovery();
                }
                catch (Exception e)
                {
                    DebugConsole.LogWarning($"[LanServer] NAT discovery failed: {e}");
                }
            }

            MY_CLIENT_ID = Utils.GetClientId(new IPEndPoint(IPAddress.Parse(ip), port));
            DebugConsole.Log($"[LanServer] MY_CLIENT_ID = {MY_CLIENT_ID} ({ip}:{port})");
            DebugConsole.Log($"[LanServer] LiteNetLib LAN server started on {ip}:{port}");

            // Connect locally to the server for the host player
            NetworkConfig.TransportClient.ConnectToHost("127.0.0.1", port);
        }

        public override void Stop()
        {
            Profiler.Scope();

            netManager?.Stop();
            netManager = null;

            if(transport.Equals(TransportType.NAT))
            {
                NatUtility.StopDiscovery();
                NatUtility.DeviceFound -= OnNatDeviceFound;
            }

            DebugConsole.Log("[LanServer] LiteNetLib LAN server stopped");
        }

        public override void Update()
        {
            Profiler.Scope();

            netManager?.PollEvents();
        }

        public override void CloseConnections()
        {
            Profiler.Scope();

            if (netManager == null)
                return;

            foreach (var peer in netManager.ConnectedPeerList)
            {
                DebugConsole.Log($"Client {Utils.GetClientId(peer)} disconnected by server shutdown.");
                peer.Disconnect();
            }

            MultiplayerSession.ConnectedPlayers.Clear();
        }

        public override void OnMessageRecieved()
        {
            Profiler.Scope();

            while (incomingPackets.TryDequeue(out var packet))
            {
                var (peer, data) = packet;

                ulong clientId = Utils.GetClientId(peer);
                int size = data.Length;

                var scope = Profiler.Scope();

                try
                {
                    PacketHandler.HandleIncoming(data);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LanServer] Failed to handle packet from {clientId}: {ex}");
                }

                scope.End(1, size);
            }
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Profiler.Scope();

            ulong clientId = Utils.GetClientId(peer);

            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out var player))
            {
                player = new MultiplayerPlayer(clientId);
                MultiplayerSession.ConnectedPlayers.Add(clientId, player);
            }

            player.Connection = peer;

            DebugConsole.Log($"[LanServer] Client connected: {clientId}");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Profiler.Scope();

            ulong clientId = Utils.GetClientId(peer);

            if (MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out var player))
            {
                player.Connection = null;
                MultiplayerSession.ConnectedPlayers.Remove(clientId);
            }

            DebugConsole.Log($"[LanServer] Client disconnected: {clientId} ({disconnectInfo.Reason})");
        }

        public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            Profiler.Scope();

            DebugConsole.Log($"[LanServer] Network error {socketError} from {endPoint}");
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Optional
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            Profiler.Scope();

            int peer_limit = Configuration.Instance.Host.MaxLobbySize;
            if (netManager.GetPeersCount(ConnectionState.Any) >= peer_limit)
            {
                DebugConsole.Log($"[LanServer] Server full rejecting");
                request.Reject();
                return;
            }
            request.AcceptIfKey("ONI_MP");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            Profiler.Scope();

            byte[] data = reader.GetRemainingBytes();
            incomingPackets.Enqueue((peer, data));

            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Profiler.Scope();

            byte[] data = reader.GetRemainingBytes();

            // Optional: handle initial handshake/hello from a client
            // Example: accept only "hello" packets
            string msg = System.Text.Encoding.UTF8.GetString(data);
            if (msg == "hello")
            {
                ulong clientId = Utils.GetClientId(remoteEndPoint);
                DebugConsole.Log($"[LanServer] Handshake received from {clientId}");
            }

            reader.Recycle();
        }

        ///////////// STUN / NAT
        private void OnNatDeviceFound(object sender, DeviceEventArgs e)
        {
            Profiler.Scope();

            try
            {
                DebugConsole.Log($"[LanServer] NAT device found: {e.Device.DeviceEndpoint}, Type:  {e.Device.NatProtocol.ToString()}");

                // Map public port to the server port
                Mapping mapping = new Mapping(Protocol.Udp, port, port, 0, "ONI_MP_LAN_Server");
                e.Device.CreatePortMap(mapping);

                DebugConsole.Log($"[LanServer] Port mapped: {e.Device.DeviceEndpoint}:{port}");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[LanServer] Port mapping failed: {ex}");
            }
        }
    }
}
