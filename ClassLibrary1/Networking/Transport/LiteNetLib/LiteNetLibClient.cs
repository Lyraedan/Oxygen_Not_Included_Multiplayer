using System;
using LiteNetLib;
using LiteNetLib.Utils;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using ONI_MP.Misc;
using System.Net;
using ONI_MP.Menus;
using UnityEngine;
using ONI_MP.Tests;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;

/*
 
    This implementation is unfinished and currently not used, but it serves as a reference for how to implement a LAN client using LiteNetLib.
    LAN Implementation finished via Riptide
 
 */
namespace ONI_MP.Networking.Transport.Lan
{
    public class LiteNetLibClient : TransportClient, INetEventListener
    {
        private int SERVER_PORT = 7777;
        private NetManager netManager;
        private NetPeer serverPeer;
        private bool connected;

        public static string HASHED_ADDRESS = string.Empty;
        public string HOST_ADDRESS = "127.0.0.1";

        public static ulong MY_CLIENT_ID = 0;
        public static bool ConnectFromConfig = false;

        private string host_ip = string.Empty;
        private int host_port = 7777;

        private readonly ConcurrentQueue<byte[]> incomingPackets = new();

        private readonly Queue<int> pingSamples = new();
        private const int JITTER_SAMPLE_COUNT = 20;

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

        public override void ConnectToHost(string ip, int port)
        {
            if (connected)
                return;

            host_ip = ip;
            host_port = port;

            netManager = new NetManager(this)
            {
                IPv6Enabled = false,
                AutoRecycle = false
            };

            netManager.Start();
            serverPeer = netManager.Connect(ip, port, "ONI_MP");
            MY_CLIENT_ID = Utils.GetClientId(new IPEndPoint(IPAddress.Parse(ip), port));
            DebugConsole.Log($"[LanClient] Connecting to {ip}:{port} with MY_CLIENT_ID = {MY_CLIENT_ID}");
            CoroutineRunner.RunOne(WaitForConnectionSuccess(10f));
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
            string ip = host_ip;
            int port = host_port;
            Disconnect();
            ConnectToHost(ip, port);
        }

        public override void OnMessageRecieved()
        {
            while (incomingPackets.TryDequeue(out var data))
            {
                int size = data.Length;
                long t0 = GameClientProfiler.Begin();

                try
                {
                    PacketHandler.HandleIncoming(data);
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[LanClient] Failed packet: {ex}");
                }

                GameClientProfiler.End(t0, 1, size);
            }
        }

        public override void Update()
        {
            netManager?.PollEvents();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            DebugConsole.Log($"[LanClient] Connected to server: {Utils.GetClientId(peer)}");

            serverPeer = peer;
            connected = true;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            DebugConsole.Log($"[LanClient] Disconnected from server ({disconnectInfo.Reason})");
            connected = false;
            host_ip = string.Empty;
            host_port = 7777;
        }

        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            DebugConsole.LogError($"[LanClient] Network error: {socketError} from {endPoint}", false);
            connected = false;
            host_ip = string.Empty;
            host_port = 7777;
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
            byte[] data = reader.GetRemainingBytes();
            incomingPackets.Enqueue(data);
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

        public override NetworkIndicatorsScreen.NetworkState GetJitterState()
        {
            if (!connected || serverPeer == null)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            int ping = serverPeer.Ping;

            pingSamples.Enqueue(ping);
            while (pingSamples.Count > JITTER_SAMPLE_COUNT)
                pingSamples.Dequeue();

            if (pingSamples.Count < 5)
                return NetworkIndicatorsScreen.NetworkState.DEGRADED;

            float mean = 0f;
            foreach (var p in pingSamples)
                mean += p;
            mean /= pingSamples.Count;

            float variance = 0f;
            foreach (var p in pingSamples)
            {
                float diff = p - mean;
                variance += diff * diff;
            }

            float jitter = Mathf.Sqrt(variance / pingSamples.Count);

            if (jitter <= 10f) return NetworkIndicatorsScreen.NetworkState.GOOD;
            if (jitter <= 30f) return NetworkIndicatorsScreen.NetworkState.DEGRADED;
            return NetworkIndicatorsScreen.NetworkState.BAD;
        }

        public override NetworkIndicatorsScreen.NetworkState GetLatencyState()
        {
            if (!connected || serverPeer == null)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            int ping = serverPeer.Ping;

            if (ping <= 60) return NetworkIndicatorsScreen.NetworkState.GOOD;
            if (ping <= 120) return NetworkIndicatorsScreen.NetworkState.DEGRADED;
            return NetworkIndicatorsScreen.NetworkState.BAD;
        }

        public override NetworkIndicatorsScreen.NetworkState GetPacketlossState()
        {
            if (!connected || serverPeer == null)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            float lossRate = serverPeer.Statistics.PacketLossPercent / 100f;
            float quality = 1f - lossRate;

            if (quality >= 0.95f) return NetworkIndicatorsScreen.NetworkState.GOOD;
            if (quality >= 0.85f) return NetworkIndicatorsScreen.NetworkState.DEGRADED;
            return NetworkIndicatorsScreen.NetworkState.BAD;
        }

        public override NetworkIndicatorsScreen.NetworkState GetServerPerformanceState()
        {
            var latency = GetLatencyState();
            var loss = GetPacketlossState();

            if (latency == NetworkIndicatorsScreen.NetworkState.BAD ||
                loss == NetworkIndicatorsScreen.NetworkState.BAD)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            if (latency == NetworkIndicatorsScreen.NetworkState.DEGRADED ||
                loss == NetworkIndicatorsScreen.NetworkState.DEGRADED)
                return NetworkIndicatorsScreen.NetworkState.DEGRADED;

            return NetworkIndicatorsScreen.NetworkState.GOOD;
        }

        IEnumerator WaitForConnectionSuccess(float timeout)
        {
            float timer = 0f;

            while (timer < timeout)
            {
                netManager?.PollEvents();

                if (connected)
                {
                    DebugConsole.Log("[LanClient] Connection successful");

                    MultiplayerOverlay.Close();

                    MultiplayerSession.SetHost(1);
                    MultiplayerSession.InSession = true;
                    PacketHandler.readyToProcess = true;

                    CoroutineRunner.RunOne(Handshake());

                    if (Utils.IsInGame())
                        NetworkConfig.TransportClient.OnContinueConnectionFlow.Invoke();
                    else
                        NetworkConfig.TransportClient.OnRequestStateOrReturn.Invoke();

                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            DebugConsole.LogWarning("[LanClient] Connection timed out");

            Disconnect();

            MultiplayerOverlay.Show(STRINGS.UI.MP_OVERLAY.CLIENT.CONNECTION_FAILED);
            yield return new WaitForSeconds(3f);
            MultiplayerOverlay.Close();
        }

        IEnumerator Handshake()
        {
            HandshakePacket handshake = new HandshakePacket();

            while (connected && serverPeer != null)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put(handshake.SerializeToByteArray()); // or your serialization

                serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);

                yield return new WaitForSeconds(1f);
            }
        }
    }
}
