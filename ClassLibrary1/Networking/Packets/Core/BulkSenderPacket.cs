using Epic.OnlineServices.P2P;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.DuplicantActions;
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
			SerializedInnerPackets = innerData;
		}

		public int InnerPacketId;
		public List<byte[]> SerializedInnerPackets = [];

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(InnerPacketId);
			int packetCount = SerializedInnerPackets.Count();
			writer.Write(packetCount);
			for (int i = 0; i < packetCount; i++)
			{
				var serializedPacket = SerializedInnerPackets[i];
				writer.Write(serializedPacket.Length);
				writer.Write(serializedPacket);
			}
			//DebugConsole.LogSuccess("Dispatching bulk packet of type " + PacketRegistry.Create(InnerPacketId).GetType().Name + " with " + SerializedInnerPackets.Count() + " packets innit");

		}
		public void Deserialize(BinaryReader reader)
		{
			InnerPacketId = reader.ReadInt32();
			int packetCount = reader.ReadInt32();
			SerializedInnerPackets = new List<byte[]>(packetCount);
			for (int i = 0; i < packetCount; i++)
			{
				int packetDataLengt = reader.ReadInt32();
				var packetData = reader.ReadBytes(packetDataLengt);
				SerializedInnerPackets.Add(packetData);
			}
		}
		public void OnDispatched()
		{
			if (!PacketRegistry.HasRegisteredPacket(InnerPacketId))
			{
				DebugConsole.LogWarning("[BulkSenderPacket] unknown inner packet id found, cannot unpack: " + InnerPacketId);
				return;
			}
			DebugConsole.Log("[BulkSenderPacket] received with "+SerializedInnerPackets.Count()+" packets of type " + PacketRegistry.Create(InnerPacketId).GetType().Name + ", dispatching");

			foreach (var packetData in SerializedInnerPackets)
			{
				var innerPacket = PacketRegistry.Create(InnerPacketId);
				var ms = new MemoryStream(packetData);
				var reader = new BinaryReader(ms);
				innerPacket.Deserialize(reader);
				innerPacket.OnDispatched();
				reader.Dispose();
				ms.Dispose();
			}
		}
	}
}
