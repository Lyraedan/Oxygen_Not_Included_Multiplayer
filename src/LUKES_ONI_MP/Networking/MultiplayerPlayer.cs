using ONI_MP.Networking;
using ONI_MP.Networking.States;
using Steamworks;

public class MultiplayerPlayer
{
    public CSteamID SteamID { get; private set; }
    public string SteamName { get; private set; }
    public bool IsLocal => SteamID == SteamUser.GetSteamID();

    public int AvatarImageId { get; private set; } = -1;
    public HSteamNetConnection? Connection { get; set; } = null;
    public bool IsConnected => Connection != null;

    public MultiplayerPlayer(CSteamID steamID)
    {
        SteamID = steamID;
        SteamName = SteamFriends.GetFriendPersonaName(steamID);
        AvatarImageId = SteamFriends.GetLargeFriendAvatar(steamID);
    }

    public override string ToString()
    {
        return $"{SteamName} ({SteamID})";
    }
}
