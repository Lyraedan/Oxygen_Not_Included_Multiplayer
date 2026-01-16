using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.States;
using Steamworks;

public class MultiplayerPlayer
{
	public ulong SteamID { get; private set; }
	public string SteamName { get; private set; }
	public bool IsLocal => SteamID == NetworkConfig.GetLocalID();

	public int AvatarImageId { get; private set; } = -1;
	//public HSteamNetConnection? Connection { get; set; } = null;
	public object? Connection { get; set; } = null;
	public bool IsConnected => Connection != null;

	public ClientReadyState readyState = ClientReadyState.Ready;

    public MultiplayerPlayer(ulong steamID)
	{
		SteamID = steamID;
		SteamName = Utils.TrucateName(SteamFriends.GetFriendPersonaName(steamID.AsCSteamID()));
		AvatarImageId = SteamFriends.GetLargeFriendAvatar(steamID.AsCSteamID());
	}

	public override string ToString()
	{
		return $"{SteamName} ({SteamID})";
	}
}
