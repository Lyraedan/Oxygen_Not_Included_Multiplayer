using ONI_MP.Networking;
using ONI_MP.Networking.Relay;
using Steamworks;

public class MultiplayerPlayer
{
    public string Id { get; private set; }
    public string Name { get; set; }
    public int AvatarImageId { get; private set; } = -1;

    /// <summary>
    /// Connection to this player.
    /// </summary>
    public INetworkConnection Connection { get; set; } = null;

    public bool IsLocal => Id == MultiplayerSession.LocalId;

    public MultiplayerPlayer(string id)
    {
        Id = id;

        if (ulong.TryParse(id, out var steamIdUlong))
        {
            var steamId = new CSteamID(steamIdUlong);
            Name = SteamFriends.GetFriendPersonaName(steamId);
            AvatarImageId = SteamFriends.GetLargeFriendAvatar(steamId);
        }
        else
        {
            // EOS or unknown platform
            Name = id;
        }
    }

    public bool IsConnected => Connection != null && Connection.IsValid;

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}
