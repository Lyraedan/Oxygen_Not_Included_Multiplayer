using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.DuplicantActions;
using ONI_MP.Patches.World;
using ONI_MP.Patches.World.Buildings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.World
{
	internal class MinionIdentitySetNamePacket : IPacket
	{
		public int NetId;
		public string NewName;

		public MinionIdentitySetNamePacket() { }
		public MinionIdentitySetNamePacket(int netId, string newName)
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
			if (!NetworkIdentityRegistry.TryGetComponent<MinionIdentity>(NetId, out var identity))
			{
				DebugConsole.LogWarning("Could not find MinionIdentity with net id " + NetId);
				return;
			}
			MinionIdentity_Patches.ApplyPacketName(identity, NewName);
		}
	}
}
