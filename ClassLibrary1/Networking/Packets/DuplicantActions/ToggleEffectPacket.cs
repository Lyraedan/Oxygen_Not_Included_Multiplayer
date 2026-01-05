using Klei.AI;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.Duplicant;
using Shared.Interfaces.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
	internal class ToggleEffectPacket : IPacket, IBulkablePacket
	{
		public int MinionNetId;
		public string EffectId;
		public bool IsAdding;
		public bool ShouldSave;

		public int MaxPackSize => 500;

		public uint IntervalMs => 50;

		public ToggleEffectPacket() { }
		public ToggleEffectPacket(Effects instance, HashedString toRemove) 
		{
			MinionNetId = instance.gameObject.AddOrGet<NetworkIdentity>().NetId;
			IsAdding = false;
			EffectId = toRemove.ToString();
		}
		public ToggleEffectPacket(Effects instance, Effect toAdd, bool shouldSave)
		{
			MinionNetId = instance.gameObject.AddOrGet<NetworkIdentity>().NetId;
			IsAdding = true;
			EffectId = toAdd.Id;
			ShouldSave = shouldSave;
		}

		public void Deserialize(BinaryReader reader)
		{
			MinionNetId = reader.ReadInt32();
			EffectId = reader.ReadString();
			IsAdding = reader.ReadBoolean();
			ShouldSave = reader.ReadBoolean();
		}
		public void Serialize(BinaryWriter writer)
		{
			writer.Write(MinionNetId);
			writer.Write(EffectId ?? string.Empty);
			writer.Write(IsAdding);
			writer.Write(ShouldSave);
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
				return;

			if(!NetworkIdentityRegistry.TryGet(MinionNetId, out var minionId))
			{
				DebugConsole.LogError("Could not find minion with net id " + MinionNetId + " to toggle effect " + EffectId + " to " + (IsAdding ? "on" : "off"), false);
			}
			if(!minionId.TryGetComponent<Effects>(out var minionEffects))
			{
				DebugConsole.LogError("Could not find effects instance on minion "+minionId.gameObject.GetProperName(), false);
			}
			if (IsAdding)
			{
				EffectsPatch.AddEffect(minionEffects, EffectId, ShouldSave);
			}
			else
			{
				EffectsPatch.RemoveEffect(minionEffects, EffectId);
			}
		}

	}
}
