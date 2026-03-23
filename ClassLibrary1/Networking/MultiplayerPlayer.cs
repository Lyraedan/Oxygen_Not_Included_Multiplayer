using ONI_MP.Misc;
using ONI_MP.Networking.States;
using Shared.Profiling;
using Steamworks;

public class MultiplayerPlayer
{
	public CSteamID SteamID { get; private set; }
	public string SteamName { get; private set; }
	public bool IsLocal => SteamID == SteamUser.GetSteamID();

	public int AvatarImageId { get; private set; } = -1;
	public HSteamNetConnection? Connection { get; set; } = null;
	public bool IsConnected => Connection != null;

	public ClientReadyState readyState = ClientReadyState.Ready;

    public MultiplayerPlayer(CSteamID steamID)
	{
		Profiler.Scope();

		SteamID = steamID;
		SteamName = Utils.TrucateName(SteamFriends.GetFriendPersonaName(steamID));
		AvatarImageId = SteamFriends.GetLargeFriendAvatar(steamID);
	}

	public override string ToString()
	{
		Profiler.Scope();

		return $"{SteamName} ({SteamID})";
	}
}
