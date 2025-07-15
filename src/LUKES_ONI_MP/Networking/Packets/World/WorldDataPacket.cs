using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ONI_MP.DebugTools;
using ONI_MP.Misc.World;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.World
{
    public class WorldDataPacket : IPacket
    {
        public PacketType Type => PacketType.WorldData;
        public List<ChunkData> Chunks = new List<ChunkData>();

        public void Serialize(BinaryWriter writer)
        {
            // First write all chunk data into a compressed memory stream
            using (var memoryStream = new MemoryStream())
            {
                using (var compressStream = new DeflateStream(memoryStream, CompressionLevel.Fastest, leaveOpen: true))
                {
                    using (var compressWriter = new BinaryWriter(compressStream))
                    {
                        compressWriter.Write(Chunks.Count);
                        foreach (var chunk in Chunks)
                        {
                            compressWriter.Write(chunk.TileX);
                            compressWriter.Write(chunk.TileY);
                            compressWriter.Write(chunk.Width);
                            compressWriter.Write(chunk.Height);
                            chunk.Serialize(compressWriter);
                        }
                    }
                }

                // Write the compressed data length followed by the compressed data
                byte[] buffer = memoryStream.ToArray();
                writer.Write(buffer.Length);
                writer.Write(buffer);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            int compressedLength = reader.ReadInt32();
            byte[] compressedData = reader.ReadBytes(compressedLength);

            using (var memoryStream = new MemoryStream(compressedData))
            using (var decompressStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
            using (var decompressReader = new BinaryReader(decompressStream))
            {
                int count = decompressReader.ReadInt32();
                Chunks = new List<ChunkData>(count);
                for (int i = 0; i < count; i++)
                {
                    var chunk = new ChunkData
                    {
                        TileX = decompressReader.ReadInt32(),
                        TileY = decompressReader.ReadInt32(),
                        Width = decompressReader.ReadInt32(),
                        Height = decompressReader.ReadInt32()
                    };
                    chunk.Deserialize(decompressReader);
                    Chunks.Add(chunk);
                }
            }
        }


        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost) return;

            foreach (var chunk in Chunks)
                chunk.Apply();

            DebugConsole.Log($"[WorldDataPacket] Applied {Chunks.Count} chunks.");

            LoadingOverlay.Clear();
        }

    }
}
