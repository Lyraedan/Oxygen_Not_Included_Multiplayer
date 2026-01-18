using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Relay
{
    public abstract class RelayClient
    {
        /// <summary>
        /// When the client is connected to the server
        /// </summary>
        public System.Action OnClientConnected;
        /// <summary>
        /// When the client is disconnected from the server
        /// </summary>
        public System.Action OnClientDisconnected;

        /// <summary>
        /// Continue the connection flow
        /// </summary>
        public System.Action OnContinueConnectionFlow;

        /// <summary>
        /// Request the game state or return to the menu
        /// </summary>
        public System.Action OnRequestStateOrReturn;
        /// <summary>
        /// Request the client to return to the menu
        /// </summary>
        public System.Action OnReturnToMenu;

        public abstract void Prepare();

        public abstract void ConnectToHost();

        public abstract void Disconnect();

        public abstract void ReconnectToSession();

        public abstract void Update();

        public abstract void OnMessageRecieved();
    }
}
