using System;
using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay;
using ONI_MP.UI;
using UnityEngine;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using ONI_MP.Networking.Relay.ONI_MP.Networking.Relay;
using ONI_MP.Networking.Relay.Platforms.EOS;
using ONI_MP.Misc;

namespace ONI_MP.Networking.Platforms.EOS
{
    public class EOSLobby : ILobby
    {
        private string _currentLobbyId;
        private readonly List<string> _members = new List<string>();
        private System.Action _onLobbyCreatedSuccess;
        private Action<string> _onLobbyJoined;
        private event Action<string> _onLobbyMembersRefreshed;

        public event Action<string> OnLobbyMembersRefreshed
        {
            add => _onLobbyMembersRefreshed += value;
            remove => _onLobbyMembersRefreshed -= value;
        }

        public string CurrentLobby => _currentLobbyId;
        public bool InLobby => !string.IsNullOrEmpty(_currentLobbyId);
        public int MaxLobbySize { get; private set; } = 0;
        public IReadOnlyList<string> LobbyMembers => _members.AsReadOnly();

        public void Initialize()
        {
            PacketRegistry.RegisterDefaults();
            DebugConsole.Log("[EOSLobby] Initialized.");
        }

        public void CreateLobby(object lobbyType = null, System.Action onSuccess = null)
        {
            MultiplayerSession.Clear();
            _onLobbyCreatedSuccess = onSuccess;

            MaxLobbySize = Configuration.GetHostProperty<int>("MaxLobbySize");
            _currentLobbyId = EOSPlatform.LocalUserId.ToString(); // Use LocalUserId string as Lobby ID

            PacketSender.Platform.GameServer.Start();
            MultiplayerSession.SetHost(_currentLobbyId);

            DebugConsole.Log($"[EOSLobby] Created lobby with LocalUserId: {_currentLobbyId}");
            _onLobbyCreatedSuccess?.Invoke();
        }

        public void LeaveLobby()
        {
            if (!InLobby) return;

            DebugConsole.Log("[EOSLobby] Leaving lobby...");

            if (MultiplayerSession.IsHost)
                PacketSender.Platform.GameServer.Shutdown();

            if (MultiplayerSession.IsClient)
                PacketSender.Platform.GameClient.Disconnect();

            _currentLobbyId = null;
            MaxLobbySize = 0;

            NetworkIdentityRegistry.Clear();
            MultiplayerSession.Clear();
        }

        public void JoinLobby(string lobbyId, Action<string> onJoinedLobby = null)
        {
            if (string.IsNullOrEmpty(lobbyId))
            {
                DebugConsole.LogError("[EOSLobby] Cannot join null or empty lobby ID.");
                return;
            }

            if (InLobby)
            {
                DebugConsole.LogWarning("[EOSLobby] Already in a lobby. Leaving first.");
                LeaveLobby();
            }

            _onLobbyJoined = onJoinedLobby;

            _currentLobbyId = lobbyId;
            MultiplayerSession.Clear();
            MultiplayerSession.SetHost(lobbyId);

            DebugConsole.Log($"[EOSLobby] Joined lobby hosted by: {lobbyId}");
            _onLobbyJoined?.Invoke(lobbyId);

            PacketSender.Platform.GameClient.ConnectToHost(lobbyId);
            RefreshLobbyMembers();
        }

        public List<string> GetAllLobbyMembers()
        {
            return new List<string>(_members);
        }

        private void RefreshLobbyMembers()
        {
            _members.Clear();

            if (Utils.IsInGame())
                MultiplayerSession.RemoveAllPlayerCursors();

            foreach (var kvp in MultiplayerSession.ConnectedPlayers)
            {
                _members.Add(kvp.Key);
                _onLobbyMembersRefreshed?.Invoke(kvp.Key);
            }

            if (Utils.IsInGame())
                MultiplayerSession.CreateConnectedPlayerCursors();
        }
    }
}
