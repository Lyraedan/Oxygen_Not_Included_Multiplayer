using ONI_MP.Api.Networking.Packets.Architecture;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Disinfect
{
	public class DisinfectPacket : IPacket
	{
		public int Cell;

		public DisinfectPacket() { }

		public DisinfectPacket(int cell)
		{
			Cell = cell;
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Cell);
		}

		public void Deserialize(BinaryReader reader)
		{
			Cell = reader.ReadInt32();
		}

		public void OnDispatched()
		{
			GameObject go = Grid.Objects[Cell, 0];
			if (go != null && go.TryGetComponent(out Disinfectable disinfectable))
			{
				disinfectable.MarkForDisinfect();
			}

			// Rebroadcast this to all clients
			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(this);
			}
		}
	}
}
