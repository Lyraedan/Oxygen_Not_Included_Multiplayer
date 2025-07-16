using Epic.OnlineServices.UserInfo;
using Epic.OnlineServices;
using System;
using ONI_MP;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.Relay.Platforms.EOS;
using Steamworks;
using static ResearchTypes;
using Epic.OnlineServices.Connect;
using ONI_MP.Networking.Packets.Architecture;

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

        int platform = Configuration.GetClientProperty<int>("Platform");
        switch(platform)
        {
            case 0:
                InitSteam(id);
                break;
            case 1:
                InitEos(id);
                break;
            default:
                InitSteam(id);
                break;
        }
    }

    private void InitSteam(string id)
    {
        Name = PacketSender.Platform.GetPlayerName(id);

        if (ulong.TryParse(id, out var steamIdUlong))
        {
            var steamId = new CSteamID(steamIdUlong);
            AvatarImageId = SteamFriends.GetLargeFriendAvatar(steamId);
        }
    }

    private void InitEos(string id)
    {
        Name = PacketSender.Platform.GetPlayerName(id);
        AvatarImageId = -1;
    }

    public bool IsConnected => Connection != null && Connection.IsValid;

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}
