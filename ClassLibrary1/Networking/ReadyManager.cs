using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.Relay.Platforms.Steam;
using ONI_MP.Networking.States;
using Steamworks;

namespace ONI_MP.Networking
{
    public class ReadyManager
    {

        private static Dictionary<string, ClientReadyState> ReadyStates = new Dictionary<string, ClientReadyState>();

        public static void SetupListeners()
        {
            PacketSender.Platform.Lobby.OnLobbyMembersRefreshed += UpdateReadyStateTracking;
        }

        public static void RunReadyCheck()
        {
            string message = "Waiting for players to be ready!\n";
            bool allReady = ReadyManager.AreAllPlayersReady(
                OnIteration: () => { MultiplayerOverlay.Show(message); },
                OnPlayerChecked: (steamName, readyState) =>
                {
                    message += $"{steamName} : {readyState}\n";
                });
            MultiplayerOverlay.Show(message);
        }

        public static void SendAllReadyPacket()
        {
            if (!MultiplayerSession.IsHost)
                return;

            //CoroutineRunner.RunOne(DelayAllReadyBroadcast());
            PacketSender.SendToAllClients(new AllClientsReadyPacket());
            AllClientsReadyPacket.ProcessAllReady();
        }

        private static System.Collections.IEnumerator DelayAllReadyBroadcast()
        {
            yield return new UnityEngine.WaitForSeconds(1f);
            PacketSender.SendToAllClients(new AllClientsReadyPacket());
            AllClientsReadyPacket.ProcessAllReady(); // Host transitions after delay
        }
        public static void SendStatusUpdatePacketToClients(string message)
        {
            if (!MultiplayerSession.IsHost)
                return;

            var packet = new ClientReadyStatusUpdatePacket
            {
                Message = message
            };
            PacketSender.SendToAllClients(packet);
        }

        public static void SendReadyStatusPacket(ClientReadyState state)
        {
            // Host is always considered ready so it doesn't send these
            if(MultiplayerSession.IsHost)
                return;
            
            var packet = new ClientReadyStatusPacket
            {
                SenderId = MultiplayerSession.LocalId,
                Status = state
            };
            PacketSender.SendToHost(packet);
        }

        /// <summary>
        /// HOST ONLY : Checks if all the players in the session are ready TODO Update to SteamLobby?
        /// </summary>
        public static bool AreAllPlayersReady(System.Action OnIteration, System.Action<string, string> OnPlayerChecked)
        {
            if (!MultiplayerSession.IsHost)
                return false;

            bool allReady = true;

            foreach (var id in PacketSender.Platform.Lobby.LobbyMembers)
            {
                OnIteration?.Invoke();

                if (id == MultiplayerSession.HostId)
                    continue;

                var state = GetPlayerReadyState(id);

                // get the name
                string name = PacketSender.Platform.GetPlayerName(id);

                // get the readable status
                string statusStr = state.ToString();

                OnPlayerChecked?.Invoke(name, statusStr);

                if (state != ClientReadyState.Ready)
                    allReady = false;
            }

            return allReady;
        }

        public static void MarkAllAsUnready()
        {
            if (!MultiplayerSession.IsHost)
                return;

            foreach (var id in PacketSender.Platform.Lobby.LobbyMembers)
            {
                if (id == MultiplayerSession.HostId)
                    continue;

                ReadyStates[id] = ClientReadyState.Unready;
            }
        }

        public static void SetPlayerReadyState(string id, ClientReadyState state)
        {
            if (id == MultiplayerSession.HostId)
                return;

            ReadyStates[id] = state;
        }

        public static ClientReadyState GetPlayerReadyState(string id)
        {
            if (id == MultiplayerSession.HostId)
                return ClientReadyState.Ready;

            return ReadyStates.TryGetValue(id, out var state) ? state : ClientReadyState.Unready;
        }


        public static void ClearReadyStates()
        {
            ReadyStates.Clear();
        }

        private static void UpdateReadyStateTracking(string id)
        {
            if (!ReadyStates.ContainsKey(id))
            {
                ReadyStates[id] = ClientReadyState.Unready;
            }

            // Clean up anyone who left
            var lobbyMembers = PacketSender.Platform.Lobby.LobbyMembers;
            var toRemove = new List<string>();
            foreach (var existing in ReadyStates.Keys)
            {
                if (!lobbyMembers.Contains(existing))
                    toRemove.Add(existing);
            }
            foreach (var remove in toRemove)
            {
                ReadyStates.Remove(remove);
            }
        }

    }
}
