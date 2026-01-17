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
	/// <summary>
	/// used by clients to broadcast a packet to all other clients via the host
	/// </summary>
	internal class HostBroadcastPacket : IPacket
	{
		public HostBroadcastPacket() { }
		public HostBroadcastPacket(IPacket innerPacket, ulong sender)
		{
			InnerPacketId = API_Helper.GetHashCode(innerPacket.GetType());
			using var ms = new MemoryStream();
			using var writer = new BinaryWriter(ms);
			innerPacket.Serialize(writer);
			InnerPacketData = ms.ToArray();
			SenderId = sender;
		}


		int InnerPacketId;
		public ulong SenderId;
		byte[] InnerPacketData;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(InnerPacketId);
			writer.Write(SenderId);
			writer.Write(InnerPacketData.Length);
			writer.Write(InnerPacketData);
		}
		public void Deserialize(BinaryReader reader)
		{
			InnerPacketId = reader.ReadInt32();
			SenderId = reader.ReadUInt64();
			int dataLength = reader.ReadInt32();
			InnerPacketData = reader.ReadBytes(dataLength);
		}

		public void OnDispatched()
		{
			if (!PacketRegistry.HasRegisteredPacket(InnerPacketId))
			{
				DebugConsole.LogWarning("[HostBroadcastPacket] unknown inner packet id found, cannot rebroadcast: "+InnerPacketId);
				return;
			}
			var innerPacket = PacketRegistry.Create(InnerPacketId);
			using var ms = new MemoryStream(InnerPacketData);
			using var reader = new BinaryReader(ms);
			innerPacket.Deserialize(reader);
			DebugConsole.Log("[HostBroadcastPacket] received packet of type " + innerPacket.GetType().Name+", dispatching");
			//this packet should only be sent by clients to the host
			if (MultiplayerSession.IsHost)
			{
				//trigger it on the host
				innerPacket.OnDispatched();
				//send it to all other clients except the sender
				PacketSender.SendToAllExcluding(innerPacket, [MultiplayerSession.HostUserID, SenderId]);
			}
		}

	}
}
