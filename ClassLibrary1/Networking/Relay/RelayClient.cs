using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Relay
{
    public abstract class RelayClient
    {
        public System.Action OnClientConnected;
        public System.Action OnClientDisconnected;

        public System.Action OnContinueConnectionFlow;

        public System.Action OnRequestStateOrReturn;
        public System.Action OnReturnToMenu;

        public abstract void Prepare();

        public abstract void ConnectToHost();

        public abstract void Disconnect();

        public abstract void ReconnectToSession();

        public abstract void Update();

        public abstract void OnMessageRecieved();
    }
}
