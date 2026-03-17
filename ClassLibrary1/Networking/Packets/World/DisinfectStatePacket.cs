using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;
using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.World
{
	public class DisinfectStatePacket : IPacket
	{
		public List<int> DisinfectCells = new List<int>();

		public void Serialize(BinaryWriter writer)
		{
			Profiler.Active.Scope();

			writer.Write(DisinfectCells.Count);
			foreach (var cell in DisinfectCells)
			{
				writer.Write(cell);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			Profiler.Active.Scope();

			int count = reader.ReadInt32();
			DisinfectCells = new List<int>(count);
			for (int i = 0; i < count; i++)
			{
				DisinfectCells.Add(reader.ReadInt32());
			}
		}

		public void OnDispatched()
		{
			Profiler.Active.Scope();

			if (MultiplayerSession.IsHost) return;
			ONI_MP.Networking.Components.WorldStateSyncer.Instance?.OnDisinfectStateReceived(this);
		}
	}
}
