using System;
using System.Net;
using Mono.Nat;
using LiteNetLib.Utils;
using LiteNetLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LiteNetLibServer : RelayServer, INetEventListener
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

        // Anything to init before start
        public override void Prepare()
        {
            writer = new NetDataWriter();
        }

        public override void Start()
        {
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
        }

        public override void Stop()
        {
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
            netManager?.PollEvents();
            DebugConsole.Log("[LanServer] Polling events");
        }

        public override void CloseConnections()
        {
            foreach (MultiplayerPlayer player in MultiplayerSession.AllPlayers)
                player.Connection = null;

            MultiplayerSession.ConnectedPlayers.Clear();
        }

        public override void OnMessageRecieved()
        {
            // LiteNetLib uses its own internal functions
        }

        public void OnPeerConnected(NetPeer peer)
        {
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
            ulong clientId = Utils.GetClientId(peer);

            if (MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out var player))
                player.Connection = null;

            MultiplayerSession.ConnectedPlayers.Remove(clientId);

            DebugConsole.Log($"[LanServer] Client disconnected: {clientId} ({disconnectInfo.Reason})");
        }

        public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            DebugConsole.Log($"[LanServer] Network error {socketError} from {endPoint}");
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Optional
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
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
            int size = reader.AvailableBytes;
            byte[] data = reader.GetRemainingBytes();

            ulong clientId = Utils.GetClientId(peer);
            long t0 = GameServerProfiler.Begin();

            try
            {
                PacketHandler.HandleIncoming(data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanServer] Failed to handle packet from {clientId}: {ex}");
            }

            GameServerProfiler.End(t0, 1, size);
            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
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
