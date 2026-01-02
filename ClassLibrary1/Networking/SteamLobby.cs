using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.ToolPatches;
using ONI_MP.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

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

		// Lobby code for the current lobby
		public static string CurrentLobbyCode { get; private set; } = "";

		// Lobby browser callback
		private static CallResult<LobbyMatchList_t> _lobbyListCallResult;
		private static Action<List<LobbyListEntry>> _onLobbyListReceived;

		private static event System.Action _onLobbyCreatedSuccess = null;
		private static event Action<CSteamID> _onLobbyJoined = null;
		private static string _pendingPassword = null;

		private static event Action<CSteamID> _OnLobbyMembersRefreshed;
		public static event Action<CSteamID> OnLobbyMembersRefreshed
		{
			add => _OnLobbyMembersRefreshed += value;
			remove => _OnLobbyMembersRefreshed -= value;
		}

		public static void Initialize()
		{
			if (!SteamManager.Initialized) return;

			try
			{
				_lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
				_lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
				_lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
				_lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

				DebugConsole.Log("[SteamLobby] Callbacks registered.");
			}
			catch (Exception ex)
			{
				DebugConsole.LogError($"[SteamLobby] Initialization failed: {ex.Message}");
				DebugConsole.LogException(ex);
			}
		}

		public static void CreateLobby(ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic, System.Action onSuccess = null)
		{
			if (!SteamManager.Initialized) return;
			//if (!GoogleDrive.Instance.IsInitialized)
			//{
			//	DebugConsole.LogWarning("[SteamLobby] Cannot create lobby. GoogleDrive needs to be initialized!");
			//	return;
			//}

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

                SelectToolPatch.UpdateColor();
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
				SteamMatchmaking.SetLobbyData(CurrentLobby, "hostname", SteamFriends.GetPersonaName());
				bool isPrivate = Configuration.Instance.Host.Lobby.IsPrivate;
				SteamMatchmaking.SetLobbyData(CurrentLobby, "visibility", isPrivate ? "private" : "public");

				// Generate and store lobby code
				CurrentLobbyCode = LobbyCodeHelper.GenerateCode(CurrentLobby);
				SteamMatchmaking.SetLobbyData(CurrentLobby, "lobby_code", CurrentLobbyCode);
				DebugConsole.Log($"[SteamLobby] Lobby code: {CurrentLobbyCode}");

				// Store lobby settings from config
				var lobbySettings = Configuration.Instance.Host.Lobby;
				SteamMatchmaking.SetLobbyData(CurrentLobby, "password_hash", lobbySettings.PasswordHash);
				SteamMatchmaking.SetLobbyData(CurrentLobby, "has_password", lobbySettings.RequirePassword ? "1" : "0");
				SteamMatchmaking.SetLobbyData(CurrentLobby, "region", GetLocalRegion());
				SteamMatchmaking.SetLobbyData(CurrentLobby, "game_id", "oni_multiplayer");

				MultiplayerSession.Clear();

				GameServer.Start();

				SteamRichPresence.SetLobbyInfo(CurrentLobby, "Multiplayer – Hosting Lobby");
				_onLobbyCreatedSuccess?.Invoke();
				_onLobbyCreatedSuccess = null;

				// Update game info if available
				UpdateGameInfo();

				//CursorManager.Instance.AssignColor();
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
			CSteamID lobbyId = callback.m_steamIDLobby;

            SteamMatchmaking.RequestLobbyData(lobbyId);
            CoroutineRunner.RunOne(CheckLobbyPasswordAfterDelay(lobbyId));
        }

        private static System.Collections.IEnumerator CheckLobbyPasswordAfterDelay(CSteamID lobbyId)
        {
            yield return new WaitForSeconds(0.5f);

            // Check if lobby requires password
            string hasPassword = SteamMatchmaking.GetLobbyData(lobbyId, "has_password");

            if (hasPassword == "1")
            {
                // Display password entry
                var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                LobbyBrowserScreen.ShowPasswordDialog(canvas.transform, lobbyId);
            }
            else
            {
                // No password needed, join directly
                JoinLobby(lobbyId);
            }
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
						//MultiplayerSession.ConnectedPlayers[user] = new MultiplayerPlayer(user);
						MultiplayerSession.ConnectedPlayers.Add(user, new MultiplayerPlayer(user));
				}
				else if (user == MultiplayerSession.HostSteamID && !MultiplayerSession.ConnectedPlayers.ContainsKey(user))
				{
					//MultiplayerSession.ConnectedPlayers[user] = new MultiplayerPlayer(user);
                    MultiplayerSession.ConnectedPlayers.Add(user, new MultiplayerPlayer(user));
                }

				DebugConsole.Log($"[SteamLobby] {name} joined the lobby.");
				ChatScreen.PendingMessage pending = ChatScreen.GeneratePendingMessage(string.Format(MP_STRINGS.UI.MP_CHATWINDOW.CHAT_CLIENT_JOINED, name));
				ChatScreen.QueueMessage(pending);
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
                ChatScreen.PendingMessage pending = ChatScreen.GeneratePendingMessage(string.Format(MP_STRINGS.UI.MP_CHATWINDOW.CHAT_CLIENT_LEFT, name));
                ChatScreen.QueueMessage(pending);
			}
		}

		public static void JoinLobby(CSteamID lobbyId, Action<CSteamID> onJoinedLobby = null, string password = null)
		{
			if (!SteamManager.Initialized)
				return;

			if (InLobby)
			{
				DebugConsole.LogWarning("[SteamLobby] Already in a lobby, leaving current one first.");
				LeaveLobby();
			}

			_onLobbyJoined = onJoinedLobby;
			_pendingPassword = password;
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
			if (Utils.IsInGame())
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
			ReadyManager.RefreshReadyState();

			if (Utils.IsInGame())
			{
				MultiplayerSession.CreateConnectedPlayerCursors();
			}
		}

		#region Lobby Code & Password

		/// <summary>
		/// Join a lobby by its lobby code.
		/// </summary>
		public static void JoinLobbyByCode(string code, string password = null, Action<CSteamID> onJoined = null, Action<string> onError = null)
		{
			if (!SteamManager.Initialized)
			{
				onError?.Invoke("Steam is not initialized");
				return;
			}

			code = LobbyCodeHelper.CleanCode(code);
			if (!LobbyCodeHelper.IsValidCodeFormat(code))
			{
				onError?.Invoke("Invalid lobby code format");
				return;
			}

			// Try to parse the code directly to a lobby ID
			if (LobbyCodeHelper.TryParseCode(code, out CSteamID lobbyId))
			{
				DebugConsole.Log($"[SteamLobby] Joining lobby by code: {code} => {lobbyId}");
				JoinLobby(lobbyId, onJoined, password);
			}
			else
			{
				onError?.Invoke("Could not parse lobby code");
			}
		}

		/// <summary>
		/// Check if the current lobby requires a password.
		/// </summary>
		public static bool LobbyRequiresPassword(CSteamID lobbyId)
		{
			string hasPassword = SteamMatchmaking.GetLobbyData(lobbyId, "has_password");
			return hasPassword == "1";
		}

		/// <summary>
		/// Validate a password against the lobby's stored hash.
		/// </summary>
		public static bool ValidateLobbyPassword(CSteamID lobbyId, string password)
		{
			string storedHash = SteamMatchmaking.GetLobbyData(lobbyId, "password_hash");
			if (string.IsNullOrEmpty(storedHash))
				return true; // No password set

			return PasswordHelper.VerifyPassword(password, storedHash);
		}

		/// <summary>
		/// Set the lobby password (host only).
		/// </summary>
		public static void SetLobbyPassword(string password)
		{
			if (!InLobby || !MultiplayerSession.IsHost)
			{
				DebugConsole.LogWarning("[SteamLobby] Cannot set password: not host or not in lobby");
				return;
			}

			string hash = string.IsNullOrEmpty(password) ? "" : PasswordHelper.HashPassword(password);
			SteamMatchmaking.SetLobbyData(CurrentLobby, "password_hash", hash);
			SteamMatchmaking.SetLobbyData(CurrentLobby, "has_password", string.IsNullOrEmpty(hash) ? "0" : "1");

			// Also update config
			Configuration.Instance.Host.Lobby.PasswordHash = hash;
			Configuration.Instance.Host.Lobby.RequirePassword = !string.IsNullOrEmpty(hash);
			Configuration.Instance.Save();

			DebugConsole.Log($"[SteamLobby] Lobby password {(string.IsNullOrEmpty(hash) ? "removed" : "set")}");
		}

		/// <summary>
		/// Set the lobby visibility (public or friends only). TODO: Later allow invite only
		/// </summary>
		public static void SetLobbyVisibility(bool isPrivate)
		{
			if (!InLobby || !MultiplayerSession.IsHost)
			{
				DebugConsole.LogWarning("[SteamLobby] Cannot set visibility: not host or not in lobby");
				return;
			}

			var lobbyType = isPrivate ? ELobbyType.k_ELobbyTypeFriendsOnly : ELobbyType.k_ELobbyTypePublic;
			SteamMatchmaking.SetLobbyType(CurrentLobby, lobbyType);
			SteamMatchmaking.SetLobbyData(CurrentLobby, "visibility", isPrivate ? "private" : "public");

			// Update config
			Configuration.Instance.Host.Lobby.IsPrivate = isPrivate;
			Configuration.Instance.Save();

			DebugConsole.Log($"[SteamLobby] Lobby visibility set to: {(isPrivate ? "Friends Only" : "Public")}");
		}

		/// <summary>
		/// Get the local user's region code.
		/// </summary>
		public static string GetLocalRegion()
		{
			// Try to get from config first
			string configRegion = Configuration.Instance.Host.Lobby.Region;
			if (!string.IsNullOrEmpty(configRegion))
				return configRegion;

			// Default fallback
			return "AUTO";
		}

		/// <summary>
		/// Update the lobby with current game info (colony name, cycle, duplicants).
		/// Should be called periodically by the host while in-game.
		/// </summary>
		public static void UpdateGameInfo()
		{
			if (!InLobby || !MultiplayerSession.IsHost)
				return;

			try
			{
				// Get colony name
				string colonyName = SaveGame.Instance?.BaseName ?? "Unknown Colony";
				SteamMatchmaking.SetLobbyData(CurrentLobby, "colony_name", colonyName);

				// Get current cycle
				int cycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0;
				SteamMatchmaking.SetLobbyData(CurrentLobby, "cycle", cycle.ToString());

				// Get duplicant counts (alive vs total)
				int aliveCount = global::Components.LiveMinionIdentities?.Count ?? 0;
				int totalCount = global::Components.MinionIdentities?.Count ?? 0;
				SteamMatchmaking.SetLobbyData(CurrentLobby, "duplicant_alive", aliveCount.ToString());
				SteamMatchmaking.SetLobbyData(CurrentLobby, "duplicant_count", totalCount.ToString());

				// Store host's ping location for client ping estimation
				float age = SteamNetworkingUtils.GetLocalPingLocation(out SteamNetworkPingLocation_t pingLocation);
				if (age >= 0)
				{
					SteamNetworkingUtils.ConvertPingLocationToString(ref pingLocation, out string locationStr, 256);
					if (!string.IsNullOrEmpty(locationStr))
					{
						SteamMatchmaking.SetLobbyData(CurrentLobby, "host_ping_location", locationStr);
					}
				}
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogWarning($"[SteamLobby] Failed to update game info: {ex.Message}");
			}
		}

		#endregion

		#region Lobby Browser

		/// <summary>
		/// Request a list of public lobbies for the browser.
		/// </summary>
		public static void RequestLobbyList(Action<List<LobbyListEntry>> onComplete)
		{
			if (!SteamManager.Initialized)
			{
				onComplete?.Invoke(new List<LobbyListEntry>());
				return;
			}

			_onLobbyListReceived = onComplete;

			// Filter for ONI Multiplayer lobbies only
			SteamMatchmaking.AddRequestLobbyListStringFilter("game_id", "oni_multiplayer", ELobbyComparison.k_ELobbyComparisonEqual);
			
			// Limit results
			SteamMatchmaking.AddRequestLobbyListResultCountFilter(50);

			var handle = SteamMatchmaking.RequestLobbyList();
			_lobbyListCallResult = CallResult<LobbyMatchList_t>.Create(OnLobbyListReceived);
			_lobbyListCallResult.Set(handle);

			DebugConsole.Log("[SteamLobby] Requesting lobby list...");
		}

		private static void OnLobbyListReceived(LobbyMatchList_t result, bool bIOFailure)
		{
			var lobbies = new List<LobbyListEntry>();

			if (bIOFailure)
			{
				DebugConsole.LogWarning("[SteamLobby] Failed to receive lobby list");
				_onLobbyListReceived?.Invoke(lobbies);
				return;
			}

			DebugConsole.Log($"[SteamLobby] Received {result.m_nLobbiesMatching} lobbies");

			for (int i = 0; i < result.m_nLobbiesMatching; i++)
			{
				CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
				if (!lobbyId.IsValid())
					continue;

				// Failsafe ignore "private" lobbies
				string visibility = SteamMatchmaking.GetLobbyData(lobbyId, "visibility");
				//if (visibility.Equals("private"))
				//	continue;

				// Get host Steam ID
				CSteamID hostSteamId = CSteamID.Nil;
				string hostStr = SteamMatchmaking.GetLobbyData(lobbyId, "host");
				if (ulong.TryParse(hostStr, out ulong hostId))
				{
					hostSteamId = new CSteamID(hostId);
				}

				// Check if host is a friend
				bool isFriend = hostSteamId.IsValid() && 
					SteamFriends.HasFriend(hostSteamId, EFriendFlags.k_EFriendFlagImmediate);

				// Estimate ping to host using their stored ping location
				int pingMs = -1;
				string hostPingLocation = SteamMatchmaking.GetLobbyData(lobbyId, "host_ping_location");
				if (!string.IsNullOrEmpty(hostPingLocation))
				{
					try
					{
						if (SteamNetworkingUtils.ParsePingLocationString(hostPingLocation, out SteamNetworkPingLocation_t location))
						{
							pingMs = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref location);
						}
					}
					catch { /* Ignore - ping estimation may fail */ }
				}

				var entry = new LobbyListEntry
				{
					LobbyId = lobbyId,
					HostSteamId = hostSteamId,
					LobbyName = SteamMatchmaking.GetLobbyData(lobbyId, "name"),
					HostName = GetHostName(lobbyId),
					PlayerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId),
					MaxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId),
					HasPassword = SteamMatchmaking.GetLobbyData(lobbyId, "has_password") == "1",
					LobbyCode = SteamMatchmaking.GetLobbyData(lobbyId, "lobby_code"),
					IsFriend = isFriend,
					IsPrivate = SteamMatchmaking.GetLobbyData(lobbyId, "visibility") == "private",
					PingMs = pingMs,
					// Game info
					ColonyName = SteamMatchmaking.GetLobbyData(lobbyId, "colony_name"),
					Cycle = int.TryParse(SteamMatchmaking.GetLobbyData(lobbyId, "cycle"), out int cycle) ? cycle : 0,
					DuplicantAlive = int.TryParse(SteamMatchmaking.GetLobbyData(lobbyId, "duplicant_alive"), out int alive) ? alive : 0,
					DuplicantTotal = int.TryParse(SteamMatchmaking.GetLobbyData(lobbyId, "duplicant_count"), out int total) ? total : 0
				};

				if (string.IsNullOrEmpty(entry.LobbyName))
					entry.LobbyName = "Unnamed Lobby";

				if (string.IsNullOrEmpty(entry.ColonyName))
					entry.ColonyName = "---";

				if (string.IsNullOrEmpty(entry.LobbyCode))
					entry.LobbyCode = LobbyCodeHelper.GenerateCode(lobbyId);

				lobbies.Add(entry);
			}

			_onLobbyListReceived?.Invoke(lobbies);
			_onLobbyListReceived = null;
		}

		private static string GetHostName(CSteamID lobbyId)
		{
			string hostStr = SteamMatchmaking.GetLobbyData(lobbyId, "host");
			if (ulong.TryParse(hostStr, out ulong hostId))
			{
				CSteamID hostSteamId = new CSteamID(hostId);
				bool isFriend = hostSteamId.IsValid() && SteamFriends.HasFriend(hostSteamId, EFriendFlags.k_EFriendFlagImmediate);
				if (isFriend)
				{
					// Return the name the user has on our friends list
					return SteamFriends.GetFriendPersonaName(new CSteamID(hostId));
                }
			}

			// Displays the users public username
            string hostname = SteamMatchmaking.GetLobbyData(lobbyId, "hostname");
			if(!string.IsNullOrEmpty(hostname))
			{
				return Utils.TrucateName(hostname); // If the name is > x then truncate
			}

			return "Unknown Host";
		}

		#endregion
	}
}

