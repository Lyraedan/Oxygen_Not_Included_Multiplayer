using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib.Utils;
using LiteNetLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using static ONI_MP.STRINGS.UI.MP_OVERLAY;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LanServer : RelayServer, INetEventListener
    {
        public enum TransportType
        {
            DIRECT,
            STUN // TODO: Research
        }

        private NetManager netManager;
        private NetDataWriter writer;

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
            int port = Configuration.Instance.Host.LanSettings.Port;

            netManager = new NetManager(this)
            {
                IPv6Enabled = false,
                UnconnectedMessagesEnabled = false,
                AutoRecycle = true
            };

            netManager.Start(port);

            MY_CLIENT_ID = Utils.GetClientId(new IPEndPoint(IPAddress.Parse(ip), port));
            DebugConsole.Log($"[LanServer] MY_CLIENT_ID = {MY_CLIENT_ID} ({ip}:{port})");

            LanPacketSender packetSender = (LanPacketSender)NetworkConfig.GetRelayPacketSender();
            //packetSender.netManager = netManager;

            DebugConsole.Log($"[LanServer] LiteNetLib LAN server started on {ip}:{port}");
        }

        public override void Stop()
        {
            netManager?.Stop();
            netManager = null;

            DebugConsole.Log("[LanServer] LiteNetLib LAN server stopped");
        }

        public override void Update()
        {
            netManager?.PollEvents();
        }

        public override void CloseConnections()
        {
            foreach (MultiplayerPlayer player in MultiplayerSession.AllPlayers)
                player.Connection = null;

            MultiplayerSession.ConnectedPlayers.Clear();
        }

        public override void OnMessageRecieved()
        {
            
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
    }
}
