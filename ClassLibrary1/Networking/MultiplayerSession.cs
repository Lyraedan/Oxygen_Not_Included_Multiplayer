using ONI_MP.DebugTools;
using ONI_MP.Misc;
using Shared;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Networking
{
	public static class MultiplayerSession
	{

		public static bool ShouldHostAfterLoad = false;

		/// <summary>
		/// HOST ONLY - Returns a list of connected players
		/// </summary>
		public static readonly Dictionary<ulong, MultiplayerPlayer> ConnectedPlayers = new Dictionary<ulong, MultiplayerPlayer>();

		public static ulong LocalSteamID => SteamUser.GetSteamID().m_SteamID;

		public static ulong HostSteamID { get; set; } = Utils.NilUlong(); // TODO Update every single god damn CSteamID to a ulong fml

		public static bool InSession = false;
		public static bool SessionHasPlayers => InSession && ConnectedPlayers.Count > 1;
		public static bool NotInSession => !InSession;

		public static bool IsHost => HostSteamID == LocalSteamID;

		public static bool IsClient => InSession && !IsHost;

		public static bool IsHostInSession => IsHost && InSession;

		public static readonly Dictionary<ulong, PlayerCursor> PlayerCursors = new Dictionary<ulong, PlayerCursor>();
		
		public static void Clear()
		{
			ConnectedPlayers.Clear();
			HostSteamID = Utils.NilUlong();
			DebugConsole.Log("[MultiplayerSession] Session cleared.");
		}

		public static void SetHost(ulong host)
		{
			HostSteamID = host;
			DebugConsole.Log($"[MultiplayerSession] Host set to: {host}");
		}

		public static MultiplayerPlayer GetPlayer(ulong id)
		{
			return ConnectedPlayers.TryGetValue(id, out var player) ? player : null;
		}

		public static MultiplayerPlayer LocalPlayer => GetPlayer(LocalSteamID);

		public static IEnumerable<MultiplayerPlayer> AllPlayers => ConnectedPlayers.Values;

		public static void CreateNewPlayerCursor(ulong steamID)
		{
			if (PlayerCursors.ContainsKey(steamID))
				return;

			var canvasGO = GameScreenManager.Instance.ssCameraCanvas;
			if (canvasGO == null)
			{
				DebugConsole.LogError("[MultiplayerSession] ssCameraCanvas is null, cannot create cursor.");
				return;
			}

			var cursorGO = new GameObject($"Cursor_{steamID}");
			cursorGO.transform.SetParent(canvasGO.transform, false);
			cursorGO.layer = LayerMask.NameToLayer("UI");

			var playerCursor = cursorGO.AddComponent<PlayerCursor>();

			playerCursor.AssignPlayer(steamID);
			playerCursor.Init();

			PlayerCursors[steamID] = playerCursor;
			DebugConsole.Log($"[MultiplayerSession] Created new cursor for {SteamFriends.GetFriendPersonaName(steamID)}");
		}

		public static void CreateConnectedPlayerCursors()
		{
			var members = SteamLobby.GetAllLobbyMembers();
			foreach (var playerId in members)
			{
				if (playerId == LocalSteamID)
					continue;

				CreateNewPlayerCursor(playerId);
			}
		}

		public static void RemovePlayerCursor(CSteamID steamID)
		{
			if (!PlayerCursors.TryGetValue(steamID, out var cursor))
				return;

			if (cursor != null && cursor.gameObject != null)
			{
				cursor.StopAllCoroutines();
				Object.Destroy(cursor.gameObject);
			}

			PlayerCursors.Remove(steamID);
			DebugConsole.Log($"[MultiplayerSession] Removed player cursor for {SteamFriends.GetFriendPersonaName(steamID)}");
		}

		public static void RemoveAllPlayerCursors()
		{
			foreach (var kvp in PlayerCursors)
			{
				var cursor = kvp.Value;
				if (cursor != null && cursor.gameObject != null)
				{
					cursor.StopAllCoroutines();
					Object.Destroy(cursor.gameObject);
				}
			}

			PlayerCursors.Clear();
			DebugConsole.Log("[MultiplayerSession] Removed all player cursors.");
		}

		public static bool TryGetCursorObject(CSteamID steamID, out GameObject cursorGO)
		{
			if (PlayerCursors.TryGetValue(steamID, out var cursor) && cursor != null)
			{
				cursorGO = cursor.gameObject;
				return true;
			}

			cursorGO = null;
			return false;
		}


	}
}
