using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;
using Shared.Profiling;

namespace ONI_MP.Networking.Packets.World
{
    public struct BuildingState
    {
        public int Cell;
        public string PrefabName;
    }

    public class BuildingStatePacket : IPacket
    {
        public List<BuildingState> Buildings = new List<BuildingState>();

        public int ChunkIndex;
        public int TotalChunks;

        public void Serialize(BinaryWriter writer)
        {
            using var _ = Profiler.Scope();

            writer.Write(ChunkIndex);
            writer.Write(TotalChunks);

            writer.Write(Buildings.Count);
            foreach (var b in Buildings)
            {
                writer.Write(b.Cell);
                writer.Write(b.PrefabName ?? string.Empty);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            using var _ = Profiler.Scope();

            ChunkIndex = reader.ReadInt32();
            TotalChunks = reader.ReadInt32();

            int count = reader.ReadInt32();
            Buildings = new List<BuildingState>(count);

            for (int i = 0; i < count; i++)
            {
                Buildings.Add(new BuildingState
                {
                    Cell = reader.ReadInt32(),
                    PrefabName = reader.ReadString()
                });
            }
        }

        public void OnDispatched()
        {
            using var _ = Profiler.Scope();

            if (MultiplayerSession.IsHost)
                return;

            Components.BuildingSyncer.Instance?.OnPacketReceived(this);
        }
    }
}