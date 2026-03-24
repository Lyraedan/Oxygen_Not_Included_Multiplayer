#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Profiling;

namespace ONI_MP.Tests
{
    public class TestPacket : IPacket
    {

        public ulong ClientID;

        public void Serialize(BinaryWriter writer)
        {
            Profiler.Scope();

            writer.Write(ClientID);
        }

        public void Deserialize(BinaryReader reader)
        {
            Profiler.Scope();

            ClientID = reader.ReadUInt64();
        }

        public void OnDispatched()
        {
            Profiler.Scope();

            DebugConsole.Log($"[TestPacket] Recieved test packet from: {ClientID}");
        }
    }
}
#endif