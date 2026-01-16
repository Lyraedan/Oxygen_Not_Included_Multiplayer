using System;
using LiteNetLib;
using LiteNetLib.Utils;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using ONI_MP.Misc;
using System.Net;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LanClient : RelayClient, INetEventListener
    {
        private int SERVER_PORT = 7777;
        private NetManager netManager;
        private NetPeer serverPeer;
        private bool connected;

        public static string HASHED_ADDRESS = string.Empty;
        public string HOST_ADDRESS = "127.0.0.1";

        public static ulong MY_CLIENT_ID = 0;
        public static bool ConnectFromConfig = false;

        public override void Prepare()
        {
            if (ConnectFromConfig)
            {
                SERVER_PORT = Configuration.Instance.Client.LanSettings.Port;
                HOST_ADDRESS = Configuration.Instance.Client.LanSettings.Ip;
            }
            else if (!string.IsNullOrEmpty(HASHED_ADDRESS))
            {
                try
                {
                    LanSettings lan = Utils.DecodeHashedAddress(HASHED_ADDRESS);
                    HOST_ADDRESS = lan.Ip;
                    SERVER_PORT = lan.Port;
                }
                catch (Exception e)
                {
                    DebugConsole.LogError("Failed to decode hashed LAN Address", false);
                }
            }
        }

        public override void ConnectToHost()
        {
            if (connected)
                return;

            netManager = new NetManager(this)
            {
                IPv6Enabled = false,
                AutoRecycle = false
            };
            netManager.Start();

            serverPeer = netManager.Connect(HOST_ADDRESS, SERVER_PORT, "ONI_MP"); // key matches server

            MY_CLIENT_ID = Utils.GetClientId(new IPEndPoint(IPAddress.Parse(HOST_ADDRESS), SERVER_PORT));

            DebugConsole.Log($"[LanClient] Connecting to {HOST_ADDRESS}:{SERVER_PORT} with MY_CLIENT_ID = {MY_CLIENT_ID}");
        }

        public override void Disconnect()
        {
            connected = false;

            if (serverPeer != null)
                serverPeer.Disconnect();

            netManager?.Stop();
            netManager = null;
            serverPeer = null;

            DebugConsole.Log("[LanClient] LAN Client disconnected");
        }

        public override void ReconnectToSession()
        {
            Disconnect();
            ConnectToHost();
        }

        public override void OnMessageRecieved()
        {
            //netManager?.PollEvents();
        }

        public override void Update()
        {
            netManager?.PollEvents();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            DebugConsole.Log($"[LanClient] Connected to server: {Utils.GetClientId(peer)}");
            connected = true;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            DebugConsole.Log($"[LanClient] Disconnected from server ({disconnectInfo.Reason})");
            connected = false;
        }

        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            DebugConsole.LogError($"[LanClient] Network error: {socketError} from {endPoint}", false);
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Optional: track latency
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            // This should not happen for a client
            request.Reject();
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            int msgCount = 1;
            int totalBytes = reader.AvailableBytes;
            long t0 = GameClientProfiler.Begin();

            byte[] data = reader.GetRemainingBytes();

            try
            {
                PacketHandler.HandleIncoming(data);
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[LanClient] Failed to handle incoming packet: {ex}");
            }

            GameClientProfiler.End(t0, msgCount, totalBytes);
            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // Only accept unconnected messages from the expected server
            if (serverPeer == null || !remoteEndPoint.Equals(serverPeer))
                return;

            int totalBytes = reader.AvailableBytes;
            long t0 = GameClientProfiler.Begin();
            byte[] data = reader.GetRemainingBytes();

            try
            {
                PacketHandler.HandleIncoming(data);
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[LanClient] Failed to handle unconnected packet: {ex}");
            }

            GameClientProfiler.End(t0, 1, totalBytes);
            reader.Recycle();
        }
    }
}
