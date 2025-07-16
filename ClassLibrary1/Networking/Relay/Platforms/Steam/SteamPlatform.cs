using System;
using System.Collections.Generic;
using System.Linq;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.Relay.Platforms.Steam;
using ONI_MP.Networking.Platforms.Steam;
using ONI_MP.Networking.Relay.ONI_MP.Networking.Relay;

namespace ONI_MP.Networking.Relay.Platforms.Steam
{
    public class SteamPlatform : INetworkPlatform
    {
        public bool IsHost => MultiplayerSession.IsHost;

        private INetworkConnection _cachedHostConnection;

        private readonly SteamGameClient _gameClient;
        private readonly SteamGameServer _gameServer;
        private readonly SteamLobby _lobby;

        public IGameClient GameClient => _gameClient;
        public IGameServer GameServer => _gameServer;

        public ILobby Lobby => _lobby;

        public SteamPlatform()
        {
            _lobby = new SteamLobby();
            _gameClient = new SteamGameClient();
            _gameServer = new SteamGameServer();
        }

        public INetworkConnection HostConnection
        {
            get
            {
                if (_cachedHostConnection != null && _cachedHostConnection.Id == MultiplayerSession.HostId)
                    return _cachedHostConnection;

                if (string.IsNullOrEmpty(MultiplayerSession.HostId))
                    return null;

                if (MultiplayerSession.ConnectedPlayers.TryGetValue(MultiplayerSession.HostId, out var host) &&
                    host.Connection is SteamConnection steamConn)
                {
                    _cachedHostConnection = steamConn;
                    return _cachedHostConnection;
                }

                return null;
            }
        }

        public string GetPlayerName(string id)
        {
            if (ulong.TryParse(id, out var steamIdRaw))
            {
                var steamId = new CSteamID(steamIdRaw);
                return SteamFriends.GetFriendPersonaName(steamId);
            }

            return id;
        }

        public IReadOnlyCollection<INetworkConnection> ConnectedClients
        {
            get
            {
                var list = new List<INetworkConnection>();

                foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
                {
                    if (player.Connection is SteamConnection steamConn)
                    {
                        list.Add(steamConn);
                    }
                }

                return list;
            }
        }

        public void SendToAll(IPacket packet, INetworkConnection exclude = null, SendType sendType = SendType.Reliable)
        {
            foreach (var conn in ConnectedClients)
            {
                if (exclude != null && conn.Id == exclude.Id)
                    continue;

                conn.Send(packet, sendType);
            }
        }

        public void SendToAllExcluding(IPacket packet, HashSet<INetworkConnection> excludeList, SendType sendType = SendType.Reliable)
        {
            foreach (var conn in ConnectedClients)
            {
                if (excludeList != null && excludeList.Any(x => x.Id == conn.Id))
                    continue;

                conn.Send(packet, sendType);
            }
        }

        public void GetJoinDialog()
        {
            SteamFriends.ActivateGameOverlay("friends");
        }

        public void GetInviteDialog()
        {
            if (ulong.TryParse(MultiplayerSession.LocalId, out ulong localId))
            {
                SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(localId));
            }
        }

    }
}
