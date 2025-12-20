using ONI_MP.Api.Networking.Packets.Architecture;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	public class StructureStatePacket : IPacket
	{
		public int Cell;
		public float Value; // Joules for Battery, Progress for others
		public bool IsActive; // Operational active state

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Cell);
			writer.Write(Value);
			writer.Write(IsActive);
		}

		public void Deserialize(BinaryReader reader)
		{
			Cell = reader.ReadInt32();
			Value = reader.ReadSingle();
			IsActive = reader.ReadBoolean();
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost) return;

			// Handled by StructureStateSyncer on client
			ONI_MP.Networking.Components.StructureStateSyncer.HandlePacket(this);
		}
	}
}
