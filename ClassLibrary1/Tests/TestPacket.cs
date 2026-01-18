using System;
using System.Collections.Generic;
using System.IO;
#if DEBUG
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Tests
{
    public class TestPacket : IPacket
    {

        public ulong ClientID;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ClientID);
        }

        public void Deserialize(BinaryReader reader)
        {
            ClientID = reader.ReadUInt64();
        }

        public void OnDispatched()
        {
            DebugConsole.Log($"Recieved test packet from: {ClientID}");
        }
    }
}
#endif