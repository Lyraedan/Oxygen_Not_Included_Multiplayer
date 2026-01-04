using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.Events
{
	internal class UserNameableChangePacket : IPacket
	{
		public int NetId;
		public string NewName;

		public UserNameableChangePacket() { }
		public UserNameableChangePacket(int netId, string newName)
		{
			NetId = netId;
			NewName = newName;
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			NewName = reader.ReadString();
		}
		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(NewName);
		}

		public void OnDispatched()
		{
			if (!NetworkIdentityRegistry.TryGetComponent<UserNameable>(NetId, out var nameable))
			{
				DebugConsole.LogWarning("Could not find UserNameable with net id " + NetId);
				return;
			}
			UserNameablePatch.ApplyPacketName(nameable, NewName);
		}
	}
}
