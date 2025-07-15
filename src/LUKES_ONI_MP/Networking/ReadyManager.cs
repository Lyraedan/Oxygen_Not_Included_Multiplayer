using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.States;
using Steamworks;

namespace ONI_MP.Networking
{
    public class ReadyManager
    {

        private static Dictionary<CSteamID, ClientReadyState> ReadyStates = new Dictionary<CSteamID, ClientReadyState>();

        public static void SetupListeners()
        {
            SteamLobby.OnLobbyMembersRefreshed += UpdateReadyStateTracking;
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
                SenderId = SteamUser.GetSteamID(),
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

            foreach (var steamId in SteamLobby.LobbyMembers)
            {
                OnIteration?.Invoke();

                if (steamId == MultiplayerSession.HostSteamID)
                    continue;

                var state = GetPlayerReadyState(steamId);

                // get the name
                string name = SteamFriends.GetFriendPersonaName(steamId);

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

            foreach (var steamId in SteamLobby.LobbyMembers)
            {
                if (steamId == MultiplayerSession.HostSteamID)
                    continue;

                ReadyStates[steamId] = ClientReadyState.Unready;
            }
        }

        public static void SetPlayerReadyState(CSteamID id, ClientReadyState state)
        {
            if (id == MultiplayerSession.HostSteamID)
                return;

            ReadyStates[id] = state;
        }

        public static ClientReadyState GetPlayerReadyState(CSteamID id)
        {
            if (id == MultiplayerSession.HostSteamID)
                return ClientReadyState.Ready;

            return ReadyStates.TryGetValue(id, out var state) ? state : ClientReadyState.Unready;
        }


        public static void ClearReadyStates()
        {
            ReadyStates.Clear();
        }

        private static void UpdateReadyStateTracking(CSteamID id)
        {
            if (!ReadyStates.ContainsKey(id))
            {
                ReadyStates[id] = ClientReadyState.Unready;
            }

            // Clean up anyone who left
            var lobbyMembers = SteamLobby.LobbyMembers;
            var toRemove = new List<CSteamID>();
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
