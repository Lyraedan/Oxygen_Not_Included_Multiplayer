using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;
using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.World
{
	public struct BuildingState
	{
		public int Cell;
		public string PrefabName;  // Changed from int PrefabHash to string for reliable lookup
	}

	public class BuildingStatePacket : IPacket
	{
		public List<BuildingState> Buildings = new List<BuildingState>();

		public void Serialize(BinaryWriter writer)
		{
			Profiler.Active.Scope();

			writer.Write(Buildings.Count);
			foreach (var b in Buildings)
			{
				writer.Write(b.Cell);
				writer.Write(b.PrefabName ?? string.Empty);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			Profiler.Active.Scope();

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
			Profiler.Active.Scope();

			if (MultiplayerSession.IsHost)
				return;

			Networking.Components.BuildingSyncer.Instance?.OnPacketReceived(this);
		}
	}
}
