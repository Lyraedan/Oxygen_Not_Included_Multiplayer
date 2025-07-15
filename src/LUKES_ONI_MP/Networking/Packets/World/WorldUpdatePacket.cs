using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.World
{
    public class WorldUpdatePacket : IPacket
    {
        public PacketType Type => PacketType.WorldUpdate;
        public List<CellUpdate> Updates = new List<CellUpdate>();

        public struct CellUpdate
        {
            public int Cell;
            public ushort ElementIdx;
            public float Temperature, Mass;
            public byte DiseaseIdx;
            public int DiseaseCount;
        }

        public void Serialize(BinaryWriter w)
        {
            using (var ms = new MemoryStream())
            {
                using (var deflate = new DeflateStream(ms, CompressionLevel.Fastest, true))
                using (var compressedWriter = new BinaryWriter(deflate))
                {
                    compressedWriter.Write(Updates.Count);
                    foreach (var u in Updates)
                    {
                        compressedWriter.Write(u.Cell);
                        compressedWriter.Write(u.ElementIdx);
                        compressedWriter.Write(u.Temperature);
                        compressedWriter.Write(u.Mass);
                        compressedWriter.Write(u.DiseaseIdx);
                        compressedWriter.Write(u.DiseaseCount);
                    }
                }

                byte[] compressedData = ms.ToArray();
                w.Write(compressedData.Length); // Write compressed length
                w.Write(compressedData);        // Write compressed payload
            }
        }

        public void Deserialize(BinaryReader r)
        {
            int compressedLength = r.ReadInt32();
            byte[] compressedData = r.ReadBytes(compressedLength);

            using (var ms = new MemoryStream(compressedData))
            using (var deflate = new DeflateStream(ms, CompressionMode.Decompress))
            using (var reader = new BinaryReader(deflate))
            {
                int count = reader.ReadInt32();
                Updates = new List<CellUpdate>(count);
                for (int i = 0; i < count; i++)
                {
                    Updates.Add(new CellUpdate
                    {
                        Cell = reader.ReadInt32(),
                        ElementIdx = reader.ReadUInt16(),
                        Temperature = reader.ReadSingle(),
                        Mass = reader.ReadSingle(),
                        DiseaseIdx = reader.ReadByte(),
                        DiseaseCount = reader.ReadInt32()
                    });
                }
            }
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost) return;

            foreach (var u in Updates)
            {
                SimMessages.ModifyCell(
                    u.Cell, u.ElementIdx,
                    u.Temperature, u.Mass,
                    u.DiseaseIdx, u.DiseaseCount,
                    SimMessages.ReplaceType.Replace
                );
            }
        }
    }
}
