using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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

        private int PORT = 8080;

        private UdpClient udp;
        private bool running;

        public static ulong MY_CLIENT_ID = 0;

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

            if (udp.Client.LocalEndPoint is IPEndPoint localEndpoint)
            {
                MY_CLIENT_ID = Utils.GetClientId(localEndpoint);
                Debug.Log($"[LanServer] MY_CLIENT_ID = {MY_CLIENT_ID} ({localEndpoint})");
            }
            else
            {
                Debug.LogWarning("[LanServer] Failed to determine local endpoint");
            }

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
            //clients.Clear();
            // TODO Update
            foreach (MultiplayerPlayer player in MultiplayerSession.AllPlayers)
            {

            }
        }

        public override void OnMessageRecieved()
        {
            if (!running || udp == null)
                return;

            long t0 = GameServerProfiler.Begin();
            int totalBytes = 0;
            int msgCount = 0;

            int maxMessagesPerPoll = Configuration.GetHostProperty<int>("MaxMessagesPerPoll");

            while (udp.Available > 0 && msgCount < maxMessagesPerPoll)
            {
                IPEndPoint remote = null;
                byte[] data;

                try
                {
                    data = udp.Receive(ref remote);
                }
                catch
                {
                    break;
                }

                msgCount++;
                totalBytes += data.Length;

                ulong clientId = Utils.GetClientId(remote);
                if (!MultiplayerSession.ConnectedPlayers.ContainsKey(clientId))
                {
                    OnClientConnected(remote, clientId);
                }
                try
                {
                    PacketHandler.HandleIncoming(data);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LanServer] Failed to handle incoming packet: {ex}");
                }
            }

            GameServerProfiler.End(t0, msgCount, totalBytes);
        }

        public void OnClientConnected(IPEndPoint remote, ulong clientId)
        {
            MultiplayerPlayer player;
            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out player))
            {
                player = new MultiplayerPlayer(clientId);
                MultiplayerSession.ConnectedPlayers.Add(clientId, player);
            }
            player.Connection = remote;

            DebugConsole.Log($"[GameServer] Connection to {clientId} fully established!");
        }
    }
}
