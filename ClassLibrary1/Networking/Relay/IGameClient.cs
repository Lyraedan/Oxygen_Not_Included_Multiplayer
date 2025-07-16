using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.States;
using Steamworks;

namespace ONI_MP.Networking.Relay
{
    public interface IGameClient
    {
        ClientState State { get; }
        bool IsHardSyncInProgress { get; set; }

        void Init();
        void ConnectToHost(string hostId, bool showLoadingScreen = true);
        void Disconnect();
        void ReconnectToSession();
        void Poll();
        void SetState(ClientState state);
        int? GetPingToHost();

        void CacheCurrentServer();
        void ReconnectFromCache();
    }
}
