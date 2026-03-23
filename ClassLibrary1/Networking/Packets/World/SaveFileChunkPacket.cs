using ONI_MP.Misc;
using ONI_MP.Misc.World;
using ONI_MP.Networking.Packets.Architecture;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	public class SaveFileChunkPacket : IPacket
	{
		public string FileName;
		public int Offset;
		public int TotalSize;
		public byte[] Chunk;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(FileName);
			writer.Write(Offset);
			writer.Write(TotalSize);
			writer.Write(Chunk.Length);
			writer.Write(Chunk);
		}

		public void Deserialize(BinaryReader reader)
		{
			FileName = reader.ReadString();
			Offset = reader.ReadInt32();
			TotalSize = reader.ReadInt32();
			int length = reader.ReadInt32();
			Chunk = reader.ReadBytes(length);
		}

		public void OnDispatched()
		{
			if (Utils.IsInGame() && !GameClient.IsHardSyncInProgress)
			{
				return;
			}

			SaveChunkAssembler.ReceiveChunk(this);
		}
	}
}
