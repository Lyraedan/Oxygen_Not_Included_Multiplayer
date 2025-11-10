using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay.ONI_MP.Networking.Relay;

namespace ONI_MP.Networking.Relay
{
    public interface INetworkPlatform
    {
        string ID { get; }
        bool IsHost { get; }

        string LocalID { get; }

        INetworkConnection HostConnection { get; }
        IReadOnlyCollection<INetworkConnection> ConnectedClients { get; }

        IGameClient GameClient { get; }
        IGameServer GameServer { get; }

        ILobby Lobby { get; }

        string GetPlayerName(string id);

        void GetJoinDialog();

        void GetInviteDialog();

        void SendToAll(IPacket packet, INetworkConnection exclude = null, SendType sendType = SendType.Reliable);
        void SendToAllExcluding(IPacket packet, HashSet<INetworkConnection> excludeList, SendType sendType = SendType.Reliable);
    }
}
