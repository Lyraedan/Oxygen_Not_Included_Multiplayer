using System;
using System.Net;
using Riptide;
using Riptide.Utils;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using ONI_MP.Misc;
using System.Collections.Concurrent;
using ONI_MP.Menus;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Networking.Transport.Lan
{
    public class RiptideClient : TransportClient
    {
        private static Client _client;

        public static Client Client
        {
            get { return _client; }
        }

        private static readonly ConcurrentQueue<byte[]> _incomingPackets = new ConcurrentQueue<byte[]>();

        // Network health
        private const int JITTER_SAMPLE_COUNT = 20;
        private readonly Queue<int> _pingSamples = new Queue<int>();

        private ConnectionMetrics Metrics => _client?.Connection?.Metrics;

        public override void Prepare()
        {
            RiptideLogger.Initialize(DebugConsole.Log, false);
        }

        public override void ConnectToHost()
        {
            if (!_client.IsNotConnected)
                return;

            string ip = Configuration.Instance.Client.LanSettings.Ip;
            int port = Configuration.Instance.Client.LanSettings.Port;
            _client = new Client("RiptideClient");
            _client.Connected += OnConnectedToServer;
            _client.Disconnected += OnDisconnectedFromServer;
            _client.MessageReceived += OnMessageRecievedFromServer;
            _client.Connect($"{ip}:{port}", useMessageHandlers: false);
        }

        private void OnMessageRecievedFromServer(object sender, MessageReceivedEventArgs e)
        {
            byte[] rawData = e.Message.GetBytes();
            _incomingPackets.Enqueue(rawData);
        }

        private void OnConnectedToServer(object sender, EventArgs e)
        {
            OnClientConnected.Invoke();
            MultiplayerSession.SetHost(1); // Host's client is always 1
            MultiplayerSession.InSession = true;

            PacketHandler.readyToProcess = true;

            if (Utils.IsInGame())
            {
                NetworkConfig.TransportClient.OnContinueConnectionFlow.Invoke();
            }
            else
            {
                NetworkConfig.TransportClient.OnRequestStateOrReturn.Invoke();
            }
        }
        private void OnDisconnectedFromServer(object sender, DisconnectedEventArgs e)
        {
            OnClientDisconnected.Invoke();
            MultiplayerSession.HostUserID = Utils.NilUlong();
            MultiplayerSession.InSession = false;
        }

        public override void Disconnect()
        {
            if (_client.IsNotConnected)
                return;

            _client.Disconnect();
        }

        public override void OnMessageRecieved()
        {
            while (_incomingPackets.TryDequeue(out var rawData))
            {
                int size = rawData.Length;

                int packetType = rawData.Length >= 4
                    ? BitConverter.ToInt32(rawData, 0)
                    : 0;

                long t0 = GameServerProfiler.Begin();

                try
                {
                    PacketHandler.HandleIncoming(rawData);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LanClient] Failed to handle packet {packetType}: {ex}");
                }

                GameServerProfiler.End(t0, 1, size);
            }
        }

        public override void ReconnectToSession()
        {
            Disconnect();
            ConnectToHost();
        }

        public override void Update()
        {
            _client?.Update();
        }

        public ulong GetClientID()
        {
            if (_client == null || _client.IsNotConnected)
                return Utils.NilUlong();

            return _client.Id;
        }

        public override NetworkIndicatorsScreen.NetworkState GetJitterState()
        {
            if (_client == null || !_client.IsConnected)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            int ping = _client.RTT;
            if (ping <= 0)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            _pingSamples.Enqueue(ping);
            while (_pingSamples.Count > JITTER_SAMPLE_COUNT)
                _pingSamples.Dequeue();

            if (_pingSamples.Count < 5)
                return NetworkIndicatorsScreen.NetworkState.DEGRADED;

            float mean = 0f;
            foreach (var p in _pingSamples)
                mean += p;
            mean /= _pingSamples.Count;

            float variance = 0f;
            foreach (var p in _pingSamples)
            {
                float diff = p - mean;
                variance += diff * diff;
            }

            float jitter = Mathf.Sqrt(variance / _pingSamples.Count);

            if (jitter <= 10f)
                return NetworkIndicatorsScreen.NetworkState.GOOD;

            if (jitter <= 30f)
                return NetworkIndicatorsScreen.NetworkState.DEGRADED;

            return NetworkIndicatorsScreen.NetworkState.BAD;
        }

        public override NetworkIndicatorsScreen.NetworkState GetLatencyState()
        {
            if (_client == null || !_client.IsConnected)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            int ping = _client.SmoothRTT;
            if (ping <= 0)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            if (ping <= 60)
                return NetworkIndicatorsScreen.NetworkState.GOOD;

            if (ping <= 120)
                return NetworkIndicatorsScreen.NetworkState.DEGRADED;

            return NetworkIndicatorsScreen.NetworkState.BAD;
        }

        public override NetworkIndicatorsScreen.NetworkState GetPacketlossState()
        {
            var metrics = Metrics;
            if (metrics == null)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            float lossRate = metrics.RollingNotifyLossRate; // 0–1
            float quality = 1f - lossRate;

            if (quality >= 0.95f)
                return NetworkIndicatorsScreen.NetworkState.GOOD;

            if (quality >= 0.85f)
                return NetworkIndicatorsScreen.NetworkState.DEGRADED;

            return NetworkIndicatorsScreen.NetworkState.BAD;
        }

        public override NetworkIndicatorsScreen.NetworkState GetServerPerformanceState()
        {
            if (_client == null || !_client.IsConnected)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            var metrics = Metrics;
            if (metrics == null)
                return NetworkIndicatorsScreen.NetworkState.BAD;

            var reliableSends = metrics.RollingReliableSends;
            
            double meanResends = reliableSends.Mean;
            double resendStdDev = reliableSends.StandardDev;

            float lossRate = metrics.RollingNotifyLossRate;
            float remoteQuality = 1f - lossRate;


            if (meanResends >= 2.0 ||           // On average needs 2+ sends per reliable
                resendStdDev >= 1.0 ||          // Highly unstable resend behavior
                remoteQuality <= 0.85f)         // Bad server quality
            {
                return NetworkIndicatorsScreen.NetworkState.BAD;
            }

            if (meanResends >= 1.2 ||            // Frequent retransmits
                resendStdDev >= 0.5 ||           // Congestion spikes
                remoteQuality <= 0.95f)          // Degraded server quality
            {
                return NetworkIndicatorsScreen.NetworkState.DEGRADED;
            }

            return NetworkIndicatorsScreen.NetworkState.GOOD;
        }
    }
}
