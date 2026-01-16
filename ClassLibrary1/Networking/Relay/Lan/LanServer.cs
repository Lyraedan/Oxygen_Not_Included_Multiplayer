using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Profiling;
using static ONI_MP.STRINGS.UI.MP_OVERLAY;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LanServer : RelayServer
    {
        public enum TransportType
        {
            DIRECT,
            STUN // TODO: Research
        }

        private ConcurrentQueue<(IPEndPoint remote, byte[] data)> packetQueue = new ConcurrentQueue<(IPEndPoint remote, byte[] data)>();

        private UdpClient udp;
        private bool running;
        private Thread listenerThread;

        public static ulong MY_CLIENT_ID = 0;

        // Anything to init before start
        public override void Prepare()
        {

        }

        public override void Start()
        {
            if (running)
                return;

            string ip = Configuration.Instance.Host.LanSettings.Ip;
            int port = Configuration.Instance.Host.LanSettings.Port;

            udp = new UdpClient(port);
            udp.Client.Blocking = false;

            MY_CLIENT_ID = Utils.GetClientId(new IPEndPoint(IPAddress.Parse(ip), port));
            DebugConsole.Log($"[LanServer] MY_CLIENT_ID = {MY_CLIENT_ID} ({ip}:{port})");

            LanPacketSender packetSender = (LanPacketSender)NetworkConfig.GetRelayPacketSender();
            packetSender.udpClient = udp;

            running = true;

            listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "LanServerListener"
            };
            listenerThread.Start();

            DebugConsole.Log($"[LanServer] LAN UDP Server started on {ip}:{port}");
        }

        public override void Stop()
        {
            udp?.Close();
            udp = null;

            // Wait for listener thread to exit
            listenerThread?.Join();
            listenerThread = null;

            running = false;
            DebugConsole.Log("[LanServer] LAN UDP Server stopped");
        }

        public override void Update()
        {
            
        }

        private void ListenLoop()
        {
            while (running && udp != null)
            {
                try
                {
                    IPEndPoint remote = null;
                    if (udp.Available > 0)
                    {
                        byte[] data = udp.Receive(ref remote);
                        if (data != null && remote != null)
                            packetQueue.Enqueue((remote, data));
                    }
                    else
                    {
                        Thread.Sleep(1); // Prevent busy waiting
                    }
                }
                catch (SocketException)
                {
                    // Non-blocking socket may throw when no data is available; safe to ignore
                }
                catch (ObjectDisposedException)
                {
                    break; // Socket closed
                }
            }
        }

        public override void CloseConnections()
        {
            foreach (MultiplayerPlayer player in MultiplayerSession.AllPlayers)
            {
                player.Connection = null;
            }
            MultiplayerSession.ConnectedPlayers.Clear();
        }

        public override void OnMessageRecieved()
        {
            if (!running || udp == null)
                return;

            // Process queued packets on main thread
            int maxMessagesPerPoll = Configuration.GetHostProperty<int>("MaxMessagesPerPoll");
            int msgCount = 0;
            int totalBytes = 0;
            long t0 = GameServerProfiler.Begin();

            while (packetQueue.TryDequeue(out var packet) && msgCount < maxMessagesPerPoll)
            {
                msgCount++;
                totalBytes += packet.data.Length;

                ulong clientId = Utils.GetClientId(packet.remote);
                if (!MultiplayerSession.ConnectedPlayers.ContainsKey(clientId))
                {
                    OnClientConnected(packet.remote, clientId);
                }

                try
                {
                    PacketHandler.HandleIncoming(packet.data);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LanServer] Failed to handle incoming packet: {ex}");
                }
            }

            if (msgCount > 0)
                GameServerProfiler.End(t0, msgCount, totalBytes);
        }

        public void OnClientConnected(IPEndPoint remote, ulong clientId)
        {
            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out var player))
            {
                player = new MultiplayerPlayer(clientId);
                MultiplayerSession.ConnectedPlayers.Add(clientId, player);
            }
            player.Connection = remote;

            DebugConsole.Log($"[GameServer] Connection to {clientId} fully established!");
        }
    }
}
