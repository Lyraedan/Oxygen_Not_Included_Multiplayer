using System;
using System.Collections.Generic;
using ONI_MP.Cloud;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.ToolPatches;
using ONI_MP.UI;
using Steamworks;
using UnityEngine.Events;

namespace ONI_MP.Networking
{
    public static class SteamLobby
    {
        private static Callback<LobbyCreated_t> _lobbyCreated;
        private static Callback<GameLobbyJoinRequested_t> _lobbyJoinRequested;
        private static Callback<LobbyEnter_t> _lobbyEntered;
        private static Callback<LobbyChatUpdate_t> _lobbyChatUpdate;

        public static readonly List<CSteamID> LobbyMembers = new List<CSteamID>();

        public static CSteamID CurrentLobby { get; private set; } = CSteamID.Nil;
        public static bool InLobby => CurrentLobby.IsValid();

        public static int MaxLobbySize { get; private set; } = 0;

        private static event System.Action _onLobbyCreatedSuccess = null;
        private static event Action<CSteamID> _onLobbyJoined = null;

        private static event Action<CSteamID> _OnLobbyMembersRefreshed;
        public static event Action<CSteamID> OnLobbyMembersRefreshed
        {
            add => _OnLobbyMembersRefreshed += value;
            remove => _OnLobbyMembersRefreshed -= value;
        }

        public static void Initialize()
        {
            if (!SteamManager.Initialized) return;

            _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
            _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

            PacketRegistry.RegisterDefaults();
            DebugConsole.Log("[SteamLobby] Callbacks registered.");
        }

        public static void CreateLobby(ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic, System.Action onSuccess = null)
        {
            if (!SteamManager.Initialized) return;
            if (!GoogleDrive.Instance.IsInitialized)
            {
                DebugConsole.LogWarning("[SteamLobby] Cannot create lobby. GoogleDrive needs to be initialized!");
                return;
            }

            if (InLobby)
            {
                DebugConsole.LogWarning("[SteamLobby] Cannot create a new lobby while already in one.");
                return;
            }
            DebugConsole.Log("[SteamLobby] Creating new lobby...");
            MaxLobbySize = Configuration.GetHostProperty<int>("MaxLobbySize");
            _onLobbyCreatedSuccess = onSuccess;
            SteamMatchmaking.CreateLobby(lobbyType, MaxLobbySize);
        }

        public static void LeaveLobby()
        {
            if (InLobby)
            {
                DebugConsole.Log("[SteamLobby] Leaving lobby...");
                if (MultiplayerSession.IsHost)
                    GameServer.Shutdown();

                if (MultiplayerSession.IsClient)
                    GameClient.Disconnect();

                NetworkIdentityRegistry.Clear();
                SteamMatchmaking.LeaveLobby(CurrentLobby);
                MultiplayerSession.Clear();
                CurrentLobby = CSteamID.Nil;
                MaxLobbySize = 0;
                SteamRichPresence.Clear();
            }
        }

        private static void OnLobbyCreated(LobbyCreated_t callback)
        {
            if (callback.m_eResult == EResult.k_EResultOK)
            {
                CurrentLobby = new CSteamID(callback.m_ulSteamIDLobby);
                DebugConsole.Log($"[SteamLobby] Lobby created: {CurrentLobby}");

                SteamMatchmaking.SetLobbyData(CurrentLobby, "name", SteamFriends.GetPersonaName() + "'s Lobby");
                SteamMatchmaking.SetLobbyData(CurrentLobby, "host", SteamUser.GetSteamID().ToString());

                MultiplayerSession.Clear();

                GameServer.Start();

                SteamRichPresence.SetLobbyInfo(CurrentLobby, "Multiplayer – Hosting Lobby");
                _onLobbyCreatedSuccess?.Invoke();
                _onLobbyCreatedSuccess = null;
                SelectToolPatch.UpdateColor();
            }
            else
            {
                DebugConsole.LogError($"[SteamLobby] Failed to create lobby: {callback.m_eResult}");
                _onLobbyCreatedSuccess = null;
            }
        }

        private static void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            DebugConsole.Log($"[SteamLobby] Joining lobby invited by {callback.m_steamIDFriend}");
            JoinLobby(callback.m_steamIDLobby);
        }

        private static void OnLobbyEntered(LobbyEnter_t callback)
        {
            CurrentLobby = new CSteamID(callback.m_ulSteamIDLobby);
            DebugConsole.Log($"[SteamLobby] Entered lobby: {CurrentLobby}");

            MultiplayerSession.Clear();

            string hostStr = SteamMatchmaking.GetLobbyData(CurrentLobby, "host");
            if (ulong.TryParse(hostStr, out ulong hostId))
            {
                MultiplayerSession.SetHost(new CSteamID(hostId));
            }

            SteamRichPresence.SetLobbyInfo(CurrentLobby, "Multiplayer – In Lobby");
            _onLobbyJoined?.Invoke(CurrentLobby);
            RefreshLobbyMembers();

            if (!MultiplayerSession.IsHost && MultiplayerSession.HostSteamID.IsValid())
            {
                GameClient.ConnectToHost(MultiplayerSession.HostSteamID);
            }
        }

        private static void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
        {
            CSteamID user = new CSteamID(callback.m_ulSteamIDUserChanged);
            EChatMemberStateChange stateChange = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;
            string name = SteamFriends.GetFriendPersonaName(user);

            if ((stateChange & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
            {
                if (MultiplayerSession.IsHost)
                {
                    if (!MultiplayerSession.ConnectedPlayers.ContainsKey(user))
                        MultiplayerSession.ConnectedPlayers[user] = new MultiplayerPlayer(user);
                }
                else if (user == MultiplayerSession.HostSteamID && !MultiplayerSession.ConnectedPlayers.ContainsKey(user))
                {
                    MultiplayerSession.ConnectedPlayers[user] = new MultiplayerPlayer(user);
                }

                DebugConsole.Log($"[SteamLobby] {name} joined the lobby.");
                ChatScreen.QueueMessage($"<color=yellow>[System]</color> <b>{name}</b> joined the game.");
            }

            if ((stateChange & EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0 ||
                (stateChange & EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0 ||
                (stateChange & EChatMemberStateChange.k_EChatMemberStateChangeKicked) != 0)
            {
                if (MultiplayerSession.ConnectedPlayers.TryGetValue(user, out var p))
                    p.Connection = null;

                MultiplayerSession.ConnectedPlayers.Remove(user);

                RefreshLobbyMembers();
                DebugConsole.Log($"[SteamLobby] {name} left the lobby.");
                ChatScreen.QueueMessage($"<color=yellow>[System]</color> <b>{name}</b> left the game.");
            }
        }

        public static void JoinLobby(CSteamID lobbyId, Action<CSteamID> onJoinedLobby = null)
        {
            if (!SteamManager.Initialized)
                return;

            if (InLobby)
            {
                DebugConsole.LogWarning("[SteamLobby] Already in a lobby, leaving current one first.");
                LeaveLobby();
            }

            _onLobbyJoined = onJoinedLobby;
            DebugConsole.Log($"[SteamLobby] Attempting to join lobby: {lobbyId}");
            SteamMatchmaking.JoinLobby(lobbyId);
        }

        public static List<CSteamID> GetAllLobbyMembers()
        {
            List<CSteamID> members = new List<CSteamID>();

            if (!InLobby) return members;

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
            for (int i = 0; i < memberCount; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i);
                members.Add(member);
            }

            return members;
        }

        private static void RefreshLobbyMembers()
        {
            LobbyMembers.Clear();
            if(Utils.IsInGame())
            {
                MultiplayerSession.RemoveAllPlayerCursors();
            }

            if (!InLobby) return;

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
            for (int i = 0; i < memberCount; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i);
                LobbyMembers.Add(member);
                _OnLobbyMembersRefreshed?.Invoke(member);
            }

            if (Utils.IsInGame())
            {
                MultiplayerSession.CreateConnectedPlayerCursors();
            }
        }

    }
}

