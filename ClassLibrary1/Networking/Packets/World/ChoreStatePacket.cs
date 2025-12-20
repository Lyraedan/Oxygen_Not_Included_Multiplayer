using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.Networking.Components;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	public enum SyncedChoreType
	{
		Mop,
		// Sweep - TODO: Implement generalized sweep sync (harder due to pickupables)
	}

	public struct ChoreData
	{
		public int Cell;
		public SyncedChoreType Type;
	}

	public class ChoreStatePacket : IPacket
	{
		public List<ChoreData> Chores = new List<ChoreData>();

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Chores.Count);
			foreach (var c in Chores)
			{
				writer.Write(c.Cell);
				writer.Write((int)c.Type);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			Chores = new List<ChoreData>(count);
			for (int i = 0; i < count; i++)
			{
				Chores.Add(new ChoreData
				{
					Cell = reader.ReadInt32(),
					Type = (SyncedChoreType)reader.ReadInt32()
				});
			}
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost) return;

			WorldStateSyncer.Instance?.OnChoreStateReceived(this);
		}
	}
}
