using Klei.AI;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.Duplicant;
using ONI_MP.Patches.KleiPatches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Core
{
	internal class ToggleAnimOverridePacket : IPacket
	{
		public int EntityNetId;
		public string Kanim;
		public bool AddingOverride;
		public float Priority = 0f;

		public ToggleAnimOverridePacket() { }
		public ToggleAnimOverridePacket(GameObject instance, KAnimFile kanim)
		{
			EntityNetId = instance.AddOrGet<NetworkIdentity>().NetId;
			AddingOverride = false;
			Kanim = kanim.name;
		}
		public ToggleAnimOverridePacket(GameObject instance, KAnimFile kanim, float priority)
		{
			EntityNetId = instance.AddOrGet<NetworkIdentity>().NetId;
			AddingOverride = true;
			Kanim = kanim.name;
			Priority = priority;
		}

		public void Deserialize(BinaryReader reader)
		{
			EntityNetId = reader.ReadInt32();
			Kanim = reader.ReadString();
			AddingOverride = reader.ReadBoolean();
			Priority = reader.ReadSingle();
		}
		public void Serialize(BinaryWriter writer)
		{
			writer.Write(EntityNetId);
			writer.Write(Kanim);
			writer.Write(AddingOverride);
			writer.Write(Priority);
		}
		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
				return;

			if (!NetworkIdentityRegistry.TryGet(EntityNetId, out var networkEntity))
			{
				DebugConsole.LogWarning("Could not find entity with net id " + EntityNetId + " to toggle AnimationOverride " + Kanim + " to " + (AddingOverride ? "on" : "off"));
			}
			if (!networkEntity.TryGetComponent<KAnimControllerBase>(out var kbac))
			{
				DebugConsole.LogWarning("Could not find KAnimControllerBaseon entity " + networkEntity.gameObject.GetProperName());
			}
			if (AddingOverride)
			{
				KAnimControllerBasePatch.AddKanimOverride(kbac, Kanim, Priority);
			}
			else
			{
				KAnimControllerBasePatch.RemoveKanimOverride(kbac, Kanim);
			}
		}
	}
}
