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

            var lobbyInterface = EOSManager.Instance.GetPlatformInterface().GetLobbyInterface();

            var createLobbyOptions = new Epic.OnlineServices.Lobby.CreateLobbyOptions
            {
                LocalUserId = EOSPlatform.LocalUserId,
                MaxLobbyMembers = (uint)MaxLobbySize,
                PermissionLevel = Epic.OnlineServices.Lobby.LobbyPermissionLevel.Publicadvertised,
                PresenceEnabled = true
            };

            lobbyInterface.CreateLobby(createLobbyOptions, null, result =>
            {
                if (result.ResultCode == Epic.OnlineServices.Result.Success)
                {
                    _currentLobbyId = result.LobbyId;
                    MultiplayerSession.SetHost(_currentLobbyId);

                    DebugConsole.Log($"[EOSLobby] Created EOS lobby successfully with ID: {_currentLobbyId}");

                    PacketSender.Platform.GameServer.Start();
                    _onLobbyCreatedSuccess?.Invoke();
                }
                else
                {
                    DebugConsole.LogError($"[EOSLobby] Failed to create EOS lobby: {result.ResultCode}");
                }
            });
        }


        public void LeaveLobby()
        {
            if (!InLobby)
                return;

            DebugConsole.Log("[EOSLobby] Leaving lobby...");

            var lobbyInterface = EOSManager.Instance.GetPlatformInterface().GetLobbyInterface();

            var leaveOptions = new Epic.OnlineServices.Lobby.LeaveLobbyOptions
            {
                LobbyId = _currentLobbyId,
                LocalUserId = EOSPlatform.LocalUserId
            };

            lobbyInterface.LeaveLobby(leaveOptions, null, result =>
            {
                if (result.ResultCode == Epic.OnlineServices.Result.Success)
                {
                    DebugConsole.Log("[EOSLobby] Successfully left EOS lobby.");
                }
                else
                {
                    DebugConsole.LogError($"[EOSLobby] Failed to leave EOS lobby: {result.ResultCode}");
                }
            });

            // Stop server/client as needed
            if (MultiplayerSession.IsHost)
                PacketSender.Platform.GameServer.Shutdown();

            if (MultiplayerSession.IsClient)
                PacketSender.Platform.GameClient.Disconnect();

            // Clear internal state
            _currentLobbyId = null;
            MaxLobbySize = 0;
            _members.Clear();

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

            const int MAX_SEARCH_RESULTS = 10;
            var lobbyInterface = EOSManager.Instance.GetPlatformInterface().GetLobbyInterface();

            lobbyInterface.CreateLobbySearch(new Epic.OnlineServices.Lobby.CreateLobbySearchOptions
            {
                MaxResults = MAX_SEARCH_RESULTS
            }, out var searchHandle);

            if (searchHandle == null)
            {
                DebugConsole.LogError("[EOSLobby] Failed to create lobby search.");
                return;
            }

            searchHandle.SetLobbyId(new Epic.OnlineServices.Lobby.LobbySearchSetLobbyIdOptions
            {
                LobbyId = lobbyId
            });

            searchHandle.Find(new Epic.OnlineServices.Lobby.LobbySearchFindOptions
            {
                LocalUserId = EOSPlatform.LocalUserId
            }, null, findResult =>
            {
                if (findResult.ResultCode != Epic.OnlineServices.Result.Success)
                {
                    DebugConsole.LogError($"[EOSLobby] Lobby search failed: {findResult.ResultCode}");
                    return;
                }

                searchHandle.CopySearchResultByIndex(new Epic.OnlineServices.Lobby.LobbySearchCopySearchResultByIndexOptions
                {
                    LobbyIndex = 0
                }, out var lobbyDetails);

                if (lobbyDetails == null)
                {
                    DebugConsole.LogError("[EOSLobby] Lobby not found after search.");
                    return;
                }

                var joinOptions = new Epic.OnlineServices.Lobby.JoinLobbyOptions
                {
                    LobbyDetailsHandle = lobbyDetails,
                    LocalUserId = EOSPlatform.LocalUserId,
                    PresenceEnabled = true
                };

                lobbyInterface.JoinLobby(joinOptions, null, joinResult =>
                {
                    if (joinResult.ResultCode == Epic.OnlineServices.Result.Success)
                    {
                        _currentLobbyId = joinResult.LobbyId;
                        MultiplayerSession.Clear();
                        MultiplayerSession.SetHost(_currentLobbyId);

                        DebugConsole.Log($"[EOSLobby] Successfully joined EOS lobby: {_currentLobbyId}");
                        _onLobbyJoined?.Invoke(_currentLobbyId);

                        PacketSender.Platform.GameClient.ConnectToHost(_currentLobbyId);
                        RefreshLobbyMembers();
                    }
                    else
                    {
                        DebugConsole.LogError($"[EOSLobby] Failed to join lobby: {joinResult.ResultCode}");
                    }
                });
            });
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
