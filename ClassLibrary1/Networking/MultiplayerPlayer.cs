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
        if (ulong.TryParse(id, out var steamIdUlong))
        {
            var steamId = new CSteamID(steamIdUlong);
            Name = SteamFriends.GetFriendPersonaName(steamId);
            AvatarImageId = SteamFriends.GetLargeFriendAvatar(steamId);
        }
    }

    private void InitEos(string id)
    {
        try
        {
            var productUserId = ProductUserId.FromString(id);
            var connectInterface = EOSManager.Instance.GetConnectInterface();

            if (connectInterface != null)
            {
                var mapOptions = new QueryProductUserIdMappingsOptions
                {
                    LocalUserId = EOSManager.Instance.GetLocalUserId(),
                    ProductUserIds = new[] { productUserId }
                };

                connectInterface.QueryProductUserIdMappings(mapOptions, null, mappingResult =>
                {
                    if (mappingResult.ResultCode == Epic.OnlineServices.Result.Success)
                    {
                        var getOptions = new GetProductUserIdMappingOptions
                        {
                            LocalUserId = EOSManager.Instance.GetLocalUserId(),
                            TargetProductUserId = productUserId,
                            AccountIdType = ExternalAccountType.Epic
                        };

                        var buffer = new System.Text.StringBuilder(256);
                        int bufferLength = buffer.Capacity;

                        if (connectInterface.GetProductUserIdMapping(getOptions, buffer, ref bufferLength) == Epic.OnlineServices.Result.Success)
                        {
                            Name = buffer.ToString();
                            DebugConsole.Log($"[EOS] Resolved display name: {Name}");
                        }
                        else
                        {
                            DebugConsole.LogError($"[EOS] Failed to get product user ID mapping for {productUserId}", false);
                            Name = "EOS_Player";
                        }
                    }
                    else
                    {
                        DebugConsole.LogError($"[EOS] QueryProductUserIdMappings failed: {mappingResult.ResultCode}", false);
                        Name = "EOS_Player";
                    }
                });
            }
        }
        catch (Exception ex)
        {
            DebugConsole.LogError($"[EOS] Failed to parse ProductUserId: {ex.Message}");
        }
    }


    public bool IsConnected => Connection != null && Connection.IsValid;

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}
