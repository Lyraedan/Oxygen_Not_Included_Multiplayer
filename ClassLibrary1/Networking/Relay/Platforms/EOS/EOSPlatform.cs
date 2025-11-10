using System;
using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay.ONI_MP.Networking.Relay;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Connect;
using ONI_MP.Networking.Platforms.Steam;
using ONI_MP.Networking.Platforms.EOS;

namespace ONI_MP.Networking.Relay.Platforms.EOS
{
    public class EOSPlatform : INetworkPlatform
    {
        string INetworkPlatform.ID => "Epic Online Services";

        public string LocalID => LocalUserId.ToString();

        public static ProductUserId LocalUserId { get; private set; }
        public static P2PInterface P2P { get; private set; }
        public static ConnectInterface Connect { get; private set; }

        private INetworkConnection _hostConnection;
        private readonly List<INetworkConnection> _connectedClients = new List<INetworkConnection>();

        public bool IsHost => MultiplayerSession.IsHost;

        public INetworkConnection HostConnection => _hostConnection;

        public IReadOnlyCollection<INetworkConnection> ConnectedClients => _connectedClients.AsReadOnly();

        private readonly EosGameClient _gameClient;
        private readonly EosGameServer _gameServer;
        private readonly EOSLobby _lobby;

        public IGameClient GameClient => _gameClient;
        public IGameServer GameServer => _gameServer;

        public ILobby Lobby => _lobby;

        public EOSPlatform()
        {
            _lobby = new EOSLobby();
            _lobby.Initialize();

            _gameClient = new EosGameClient();
            _gameServer = new EosGameServer();
        }

        public static void Initialize(ProductUserId localUserId, P2PInterface p2pInterface, ConnectInterface connectInterface)
        {
            LocalUserId = localUserId;
            P2P = p2pInterface;
            Connect = connectInterface;

            DebugConsole.Log($"[EOSPlatform] Initialized with LocalUserId = {LocalUserId}");
        }

        public void SetHostConnection(INetworkConnection connection)
        {
            _hostConnection = connection;
        }

        public void AddClient(INetworkConnection client)
        {
            if (!_connectedClients.Contains(client))
                _connectedClients.Add(client);
        }

        public void RemoveClient(INetworkConnection client)
        {
            _connectedClients.Remove(client);
        }

        public string GetPlayerName(string id)
        {
            ProductUserId productUserId;

            try
            {
                productUserId = ProductUserId.FromString(id);
            }
            catch (Exception)
            {
                return $"EOS_User_{id}";
            }

            var options = new CopyProductUserInfoOptions
            {
                TargetUserId = productUserId
            };

            var result = Connect.CopyProductUserInfo(options, out var userInfo);

            if (result == Epic.OnlineServices.Result.Success && userInfo != null && !string.IsNullOrEmpty(userInfo.DisplayName))
            {
                return userInfo.DisplayName;
            }

            return $"EOS_User_{id}";
        }


        public void GetJoinDialog()
        {
            var ui = EOSManager.Instance.GetPlatformInterface()?.GetUIInterface();
            if (ui == null)
            {
                DebugConsole.LogError("[EOSPlatform] UI Interface is not available.");
                return;
            }

            var options = new Epic.OnlineServices.UI.ShowFriendsOptions
            {
                LocalUserId = EOSManager.Instance.GetEpicAccountId()
            };

            ui.ShowFriends(options, null, result =>
            {
                if (result.ResultCode == Epic.OnlineServices.Result.Success)
                {
                    DebugConsole.Log("[EOSPlatform] Friends overlay shown. Join invitations can be accepted there.");
                }
                else
                {
                    DebugConsole.LogError($"[EOSPlatform] Failed to show friends UI: {result.ResultCode}");
                }
            });
        }

        public void GetInviteDialog()
        {
            var ui = EOSManager.Instance.GetPlatformInterface()?.GetUIInterface();
            if (ui == null)
            {
                DebugConsole.LogError("[EOSPlatform] UI Interface is not available.");
                return;
            }

            var options = new Epic.OnlineServices.UI.ShowFriendsOptions
            {
                LocalUserId = EOSManager.Instance.GetEpicAccountId()
            };

            ui.ShowFriends(options, null, result =>
            {
                if (result.ResultCode == Epic.OnlineServices.Result.Success)
                {
                    DebugConsole.Log("[EOSPlatform] Invite dialog (friends list) shown.");
                }
                else
                {
                    DebugConsole.LogError($"[EOSPlatform] Failed to show invite dialog: {result.ResultCode}");
                }
            });
        }

        public void SendToAll(IPacket packet, INetworkConnection exclude = null, SendType sendType = SendType.Reliable)
        {
            foreach (var client in _connectedClients)
            {
                if (client != exclude && client.IsValid)
                {
                    client.Send(packet, sendType);
                }
            }
        }

        public void SendToAllExcluding(IPacket packet, HashSet<INetworkConnection> excludeList, SendType sendType = SendType.Reliable)
        {
            foreach (var client in _connectedClients)
            {
                if (!excludeList.Contains(client) && client.IsValid)
                {
                    client.Send(packet, sendType);
                }
            }
        }
    }
}
