using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ONI_MP.Networking.States;

namespace ONI_MP.Networking.Relay
{
    public interface IGameServer
    {
        ServerState State { get; }

        /// <summary>Starts the game server and begins accepting connections.</summary>
        void Start();

        /// <summary>Gracefully shuts down the server, closing all connections.</summary>
        void Shutdown();

        /// <summary>Called each frame to process networking events.</summary>
        void Update();
    }
}
