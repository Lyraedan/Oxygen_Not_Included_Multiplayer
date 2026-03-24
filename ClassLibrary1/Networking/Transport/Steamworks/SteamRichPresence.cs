using ONI_MP.DebugTools;
using Steamworks;

namespace ONI_MP.Networking.Transport.Steamworks
{
	public static class SteamRichPresence
	{
		public static void SetStatus(string status)
		{
			Profiler.Scope();

			if (!SteamManager.Initialized)
			{
				DebugConsole.LogWarning("SteamRichPresence: Not initialized.");
				return;
			}

			SteamFriends.SetRichPresence("gamestatus", status);
			DebugConsole.Log($"SteamRichPresence: Status set to \"{status}\"");
		}

		public static void Clear()
		{
			Profiler.Scope();

			if (!SteamManager.Initialized)
			{
				DebugConsole.LogWarning("SteamRichPresence: Not initialized.");
				return;
			}

			SteamFriends.ClearRichPresence();
			DebugConsole.Log("SteamRichPresence: Cleared.");
		}

		public static void SetLobbyInfo(CSteamID lobby, string status)
		{
			Profiler.Scope();

			SteamFriends.ClearRichPresence();

			SteamFriends.SetRichPresence("gamestatus", "In Multiplayer Lobby");
			SteamFriends.SetRichPresence("steam_display", "Lobby");
			SteamFriends.SetRichPresence("steam_player_group", SteamLobby.CurrentLobby.ToString());
			int group_size = SteamMatchmaking.GetNumLobbyMembers(SteamLobby.CurrentLobby);
			SteamFriends.SetRichPresence("steam_player_group_size", $"{group_size}");

		}
	}
}
