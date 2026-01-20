using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.Core
{
    public class DedicatedServerMessagePacket : IPacket
    {
        public int PacketID;
        public byte[] PacketData;
        public int SendType; // Reliable, Unreliable

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PacketID);
            writer.Write(SendType);
            writer.Write(PacketData.Length);
            writer.Write(PacketData);
        }

        public void Deserialize(BinaryReader reader)
        {
            PacketID = reader.ReadInt32();
            SendType = reader.ReadInt32();
            int length = reader.ReadInt32();
            PacketData = reader.ReadBytes(length);
        }

        public void OnDispatched()
        {
            if (!PacketRegistry.HasRegisteredPacket(PacketID))
            {
                DebugConsole.LogWarning("Received a non-registered packet from the dedicated server");
                return;
            }

            var packet = PacketRegistry.Create(PacketID);

            using (var ms = new MemoryStream(PacketData))
            using (var reader = new BinaryReader(ms))
            {
                packet.Deserialize(reader);
            }

            packet.OnDispatched();
        }
    }
}
