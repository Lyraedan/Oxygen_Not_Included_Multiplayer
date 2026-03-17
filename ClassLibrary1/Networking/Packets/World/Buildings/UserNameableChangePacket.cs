using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.World.Buildings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.World.Buildings
{
	internal class UserNameableChangePacket : IPacket
	{
		public int NetId;
		public string NewName;

		public UserNameableChangePacket() { }
		public UserNameableChangePacket(int netId, string newName)
		{
			Profiler.Active.Scope();

			NetId = netId;
			NewName = newName;
		}

		public void Deserialize(BinaryReader reader)
		{
			Profiler.Active.Scope();

			NetId = reader.ReadInt32();
			NewName = reader.ReadString();
		}
		public void Serialize(BinaryWriter writer)
		{
			Profiler.Active.Scope();

			writer.Write(NetId);
			writer.Write(NewName);
		}

		public void OnDispatched()
		{
			Profiler.Active.Scope();

			if (!NetworkIdentityRegistry.TryGetComponent<UserNameable>(NetId, out var nameable))
			{
				DebugConsole.LogWarning("Could not find UserNameable with net id " + NetId);
				return;
			}
			UserNameablePatch.ApplyPacketName(nameable, NewName);
			Utils.RefreshIfSelected(nameable);
		}
	}
}
