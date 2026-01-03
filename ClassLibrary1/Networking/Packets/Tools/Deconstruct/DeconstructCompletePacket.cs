using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Deconstruct
{
	public class DeconstructCompletePacket : IPacket
	{
		public int Cell, ObjectLayer;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Cell);
			writer.Write(ObjectLayer);
		}

		public void Deserialize(BinaryReader reader)
		{
			Cell = reader.ReadInt32();
			ObjectLayer = reader.ReadInt32();
		}

		public void OnDispatched()
		{
			if (!Grid.IsValidCell(Cell))
				return;

			GameObject go = Grid.Objects[Cell, ObjectLayer];
			if (go == null)
				return;

			if (go.TryGetComponent<Deconstructable>(out var deconstructable) && !deconstructable.HasBeenDestroyed)
			{
				DebugConsole.Log($"[DeconstructCompletePacket] Forcing deconstruct at cell {Cell} on objectlayer {ObjectLayer} on client.");
				deconstructable.ForceDestroyAndGetMaterials();
			}
		}
	}
}
