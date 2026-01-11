using Klei.AI;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static STRINGS.UI.OUTFITS;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
	// Host -> Client only. Vitals are simulated on Host.
	public class VitalStatsPacket : IPacket
	{
		Dictionary<string, float> VitalAmounts = [];
		public byte TargetDiseaseIdx;
		public int TargetDiseaseCount;
		public int NetId;

		public VitalStatsPacket() { }
		public VitalStatsPacket(int netId, Amounts amounts, PrimaryElement element)
		{
			NetId = netId;
			TargetDiseaseIdx = element.DiseaseIdx;
			TargetDiseaseCount = element.DiseaseCount;

		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(TargetDiseaseIdx);
			writer.Write(TargetDiseaseCount);

			writer.Write(VitalAmounts.Count);
			foreach (var kvp in VitalAmounts)
			{
				writer.Write(kvp.Key);
				writer.Write(kvp.Value);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			TargetDiseaseIdx = reader.ReadByte();
			TargetDiseaseCount = reader.ReadInt32();
			int amountsCount = reader.ReadInt32();
			VitalAmounts = new Dictionary<string, float>(amountsCount);
			for (int i = 0; i < amountsCount; i++)
			{
				string key = reader.ReadString();
				float value = reader.ReadSingle();
				VitalAmounts[key] = value;
			}
		}

		public void OnDispatched()
		{
			// Only Clients apply this
			if (MultiplayerSession.IsHost) return;
			Apply();
		}

		private void Apply()
		{
			if (!NetworkIdentityRegistry.TryGet(NetId, out var identity)) return;

			var amounts = identity.GetAmounts();
			if (amounts == null) return;

			foreach (var kvp in VitalAmounts)
			{
				amounts.SetValue(kvp.Key, kvp.Value);
			}
			if (identity.TryGetComponent<PrimaryElement>(out var element))
			{
				int currentDiseaseCount = element.DiseaseCount;
				int currentDiseaseIdx = element.DiseaseIdx;
				if (currentDiseaseIdx != TargetDiseaseIdx)
				{
					element.AddDisease(TargetDiseaseIdx, TargetDiseaseCount, "MP-Mod.SyncedDisease");
				}
				else if (!Mathf.Approximately(currentDiseaseCount, TargetDiseaseCount))
					element.ModifyDiseaseCount(TargetDiseaseCount - currentDiseaseCount, "MP-Mod.SyncedDisease");
			}
		}
	}
}
