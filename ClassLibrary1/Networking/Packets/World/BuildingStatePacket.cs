using ONI_MP.Api.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	public struct BuildingState
	{
		public int Cell;
		public int PrefabHash;
	}

	public class BuildingStatePacket : IPacket
	{
		public List<BuildingState> Buildings = new List<BuildingState>();

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Buildings.Count);
			foreach (var b in Buildings)
			{
				writer.Write(b.Cell);
				writer.Write(b.PrefabHash);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			Buildings = new List<BuildingState>(count);
			for (int i = 0; i < count; i++)
			{
				Buildings.Add(new BuildingState
				{
					Cell = reader.ReadInt32(),
					PrefabHash = reader.ReadInt32()
				});
			}
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
				return;

			Networking.Components.BuildingSyncer.Instance?.OnPacketReceived(this);
		}
	}
}
