using System;
using System.Collections.Generic;
using ONI_MP.Cloud;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets;
using ONI_MP.Patches.ToolPatches;
using ONI_MP.UI;
using Steamworks;
using UnityEngine;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay.ONI_MP.Networking.Relay;

namespace ONI_MP.Networking.Platforms.Steam
{
    public class SteamLobby : ILobby
    {
        private Callback<LobbyCreated_t> _lobbyCreated;
        private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequested;
        private Callback<LobbyEnter_t> _lobbyEntered;
        private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;

        private readonly List<CSteamID> _lobbyMembers = new List<CSteamID>();
        private System.Action _onLobbyCreatedSuccess;
        private Action<string> _onLobbyJoined;

        private event Action<string> _onLobbyMembersRefreshed;
        public event Action<string> OnLobbyMembersRefreshed
        {
            add => _onLobbyMembersRefreshed += value;
            remove => _onLobbyMembersRefreshed -= value;
        }

        private CSteamID _currentLobby = CSteamID.Nil;
        public string CurrentLobby => _currentLobby.IsValid() ? _currentLobby.ToString() : null;

        public bool InLobby => _currentLobby.IsValid();

        public int MaxLobbySize { get; private set; } = 0;

        public IReadOnlyList<string> LobbyMembers => _lobbyMembers.ConvertAll(id => id.ToString());

        public void Initialize()
        {
            if (!SteamManager.Initialized) return;

            _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
            _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

            PacketRegistry.RegisterDefaults();
            DebugConsole.Log("[SteamLobby] Callbacks registered.");
        }

        public void CreateLobby(object lobbyType = null, System.Action onSuccess = null)
        {
            if (!SteamManager.Initialized || !GoogleDrive.Instance.IsInitialized) return;

            if (InLobby)
            {
                DebugConsole.LogWarning("[SteamLobby] Already in a lobby.");
                return;
            }

            DebugConsole.Log("[SteamLobby] Creating new lobby...");
            MaxLobbySize = Configuration.GetHostProperty<int>("MaxLobbySize");

            _onLobbyCreatedSuccess = onSuccess;

            SteamMatchmaking.CreateLobby(
                lobbyType is ELobbyType type ? type : ELobbyType.k_ELobbyTypePublic,
                MaxLobbySize
            );
        }

        public void LeaveLobby()
        {
            if (!InLobby) return;

            DebugConsole.Log("[SteamLobby] Leaving lobby...");

            if (MultiplayerSession.IsHost)
                PacketSender.Platform.GameServer.Shutdown();

            if (MultiplayerSession.IsClient)
                PacketSender.Platform.GameClient.Disconnect();

            NetworkIdentityRegistry.Clear();
            SteamMatchmaking.LeaveLobby(_currentLobby);
            MultiplayerSession.Clear();
            _currentLobby = CSteamID.Nil;
            MaxLobbySize = 0;
            SteamRichPresence.Clear();
        }

        public void JoinLobby(string lobbyId, Action<string> onJoinedLobby = null)
        {
            if (!SteamManager.Initialized) return;

            if (!ulong.TryParse(lobbyId, out var parsed))
            {
                DebugConsole.LogError($"[SteamLobby] Invalid lobby ID: {lobbyId}");
                return;
            }

            if (InLobby)
            {
                DebugConsole.LogWarning("[SteamLobby] Already in lobby, leaving first.");
                LeaveLobby();
            }

            _onLobbyJoined = onJoinedLobby;

            var steamLobbyId = new CSteamID(parsed);
            DebugConsole.Log($"[SteamLobby] Joining lobby {steamLobbyId}...");
            SteamMatchmaking.JoinLobby(steamLobbyId);
        }

        public List<string> GetAllLobbyMembers()
        {
            var result = new List<string>();
            if (!InLobby) return result;

            int count = SteamMatchmaking.GetNumLobbyMembers(_currentLobby);
            for (int i = 0; i < count; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobby, i);
                result.Add(member.ToString());
            }

            return result;
        }

        private void OnLobbyCreated(LobbyCreated_t cb)
        {
            if (cb.m_eResult != EResult.k_EResultOK)
            {
                DebugConsole.LogError($"[SteamLobby] Failed to create lobby: {cb.m_eResult}");
                _onLobbyCreatedSuccess = null;
                return;
            }

            _currentLobby = new CSteamID(cb.m_ulSteamIDLobby);
            DebugConsole.Log($"[SteamLobby] Lobby created: {CurrentLobby}");

            SteamMatchmaking.SetLobbyData(_currentLobby, "name", SteamFriends.GetPersonaName() + "'s Lobby");
            string host = SteamUser.GetSteamID().ToString();
            SteamMatchmaking.SetLobbyData(_currentLobby, "host", host);

            MultiplayerSession.SetHost(host);

            MultiplayerSession.Clear();
            PacketSender.Platform.GameServer.Start();

            SteamRichPresence.SetLobbyInfo(_currentLobby, "Multiplayer – Hosting Lobby");

            _onLobbyCreatedSuccess?.Invoke();
            _onLobbyCreatedSuccess = null;

            SelectToolPatch.UpdateColor();
        }

        private void OnLobbyJoinRequested(GameLobbyJoinRequested_t cb)
        {
            DebugConsole.Log($"[SteamLobby] Received invite from {cb.m_steamIDFriend}");
            JoinLobby(cb.m_steamIDLobby.ToString());
        }

        private void OnLobbyEntered(LobbyEnter_t cb)
        {
            _currentLobby = new CSteamID(cb.m_ulSteamIDLobby);
            DebugConsole.Log($"[SteamLobby] Entered lobby: {CurrentLobby}");

            MultiplayerSession.Clear();

            string hostStr = SteamMatchmaking.GetLobbyData(_currentLobby, "host");
            MultiplayerSession.SetHost(hostStr);
            CSteamID hostSteamID = CSteamID.Nil;

            if (ulong.TryParse(hostStr, out var hostId))
            {
                hostSteamID = new CSteamID(hostId);
            }

            SteamRichPresence.SetLobbyInfo(_currentLobby, "Multiplayer – In Lobby");

            _onLobbyJoined?.Invoke(CurrentLobby.ToString());
            RefreshLobbyMembers();

            if (!MultiplayerSession.IsHost && hostSteamID.IsValid())
                PacketSender.Platform.GameClient.ConnectToHost(hostSteamID.ToString());
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t cb)
        {
            var user = new CSteamID(cb.m_ulSteamIDUserChanged).ToString();
            var change = (EChatMemberStateChange)cb.m_rgfChatMemberStateChange;
            var name = PacketSender.Platform.GetPlayerName(user);

            if ((change & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
            {
                if (!MultiplayerSession.ConnectedPlayers.ContainsKey(user))
                    MultiplayerSession.ConnectedPlayers[user] = new MultiplayerPlayer(user);

                DebugConsole.Log($"[SteamLobby] {name} joined the lobby.");
                ChatScreen.QueueMessage($"<color=yellow>[System]</color> <b>{name}</b> joined the game.");
            }

            if ((change & EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0 ||
                (change & EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0 ||
                (change & EChatMemberStateChange.k_EChatMemberStateChangeKicked) != 0)
            {
                MultiplayerSession.ConnectedPlayers.Remove(user);
                DebugConsole.Log($"[SteamLobby] {name} left the lobby.");
                ChatScreen.QueueMessage($"<color=yellow>[System]</color> <b>{name}</b> left the game.");
                RefreshLobbyMembers();
            }
        }

        private void RefreshLobbyMembers()
        {
            _lobbyMembers.Clear();

            if (Utils.IsInGame())
                MultiplayerSession.RemoveAllPlayerCursors();

            if (!InLobby)
                return;

            int count = SteamMatchmaking.GetNumLobbyMembers(_currentLobby);
            for (int i = 0; i < count; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobby, i);
                _lobbyMembers.Add(member);
                _onLobbyMembersRefreshed?.Invoke(member.ToString());
            }

            if (Utils.IsInGame())
                MultiplayerSession.CreateConnectedPlayerCursors();
        }
    }
}
