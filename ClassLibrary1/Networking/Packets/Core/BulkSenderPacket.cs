using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.Core
{
	internal class BulkSenderPacket : IPacket
	{
		public BulkSenderPacket() { }
		public BulkSenderPacket(int packetId, List<byte[]> innerData)
		{
			InnerPacketId = packetId;
			InnerPacketsData = innerData;
			DebugConsole.LogSuccess("Dispatching bulk packet of type " + PacketRegistry.Create(packetId).GetType().Name  + " with " + innerData.Count() + " packets innit");
		}

		public int InnerPacketId;
		public List<byte[]> InnerPacketsData = [];

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(InnerPacketId);
			writer.Write(InnerPacketsData.Count());
			for (int i = 0; i < InnerPacketsData.Count(); i++)
			{
				writer.Write(InnerPacketsData[i]);
			}
		}
		public void Deserialize(BinaryReader reader)
		{
			InnerPacketId = reader.ReadInt32();
			int dataLength = reader.ReadInt32();
			InnerPacketsData = new List<byte[]>(dataLength);
			for (int i = 0; i < dataLength; i++)
			{
				InnerPacketsData.Add(reader.ReadBytes(reader.ReadInt32()));
			}
		}
		public void OnDispatched()
		{
			if (!PacketRegistry.HasRegisteredPacket(InnerPacketId))
			{
				DebugConsole.LogWarning("[BulkSenderPacket] unknown inner packet id found, cannot unpack: " + InnerPacketId);
				return;
			}
			DebugConsole.Log("[BulkSenderPacket] received with "+InnerPacketsData.Count()+" packets of type " + PacketRegistry.Create(InnerPacketId).GetType().Name + ", dispatching");
			foreach (var packetData in InnerPacketsData)
			{
				var innerPacket = PacketRegistry.Create(InnerPacketId);
				using var ms = new MemoryStream(packetData);
				using var reader = new BinaryReader(ms);
				innerPacket.Deserialize(reader);
				innerPacket.OnDispatched();
			}
		}
	}
}
