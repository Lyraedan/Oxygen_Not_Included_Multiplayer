using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Relay
{
    public interface INetworkConnection
    {
        bool IsValid { get; }
        string DebugName { get; }

        string Id { get; }

        object RawHandle { get; }

        void Send(IPacket packet, SendType sendType = SendType.Reliable);
    }
}
