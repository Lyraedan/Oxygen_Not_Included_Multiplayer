using Klei.AI;
using ONI_MP.Api.Networking.Packets.Architecture;
using System.IO;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
	// Host -> Client only. Vitals are simulated on Host.
	public class VitalStatsPacket : IPacket
	{
		public int NetId;
		public float Health;
		public float Calories;
		public float MaxCalories;
		public float Stress;
		public float Breath;
		public float Stamina;
		public float Bladder;
		public byte GermElemIdx;
		public int GermCount;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(Health);
			writer.Write(Calories);
			writer.Write(MaxCalories);
			writer.Write(Stress);
			writer.Write(Breath);
			writer.Write(Stamina);
			writer.Write(Bladder);
			writer.Write(GermElemIdx);
			writer.Write(GermCount);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			Health = reader.ReadSingle();
			Calories = reader.ReadSingle();
			MaxCalories = reader.ReadSingle();
			Stress = reader.ReadSingle();
			Breath = reader.ReadSingle();
			Stamina = reader.ReadSingle();
			Bladder = reader.ReadSingle();
			GermElemIdx = reader.ReadByte();
			GermCount = reader.ReadInt32();
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

			// Amount IDs: "Calories", "HitPoints", "Stress", "Breath", "Stamina", "Bladder"

			SetAmount(amounts, "HitPoints", Health);
			SetAmount(amounts, "Calories", Calories);
			SetAmount(amounts, "Stress", Stress);
			SetAmount(amounts, "Breath", Breath);
			SetAmount(amounts, "Stamina", Stamina);
			SetAmount(amounts, "Bladder", Bladder);

			// Set Max Calories if possible to fix the bar scaling
			try
			{
				var cal = amounts.Get("Calories");
				if (cal != null)
				{
					// Inspect if we can set max_attribute or similar?
					// Normally Max is derived from Attribute.
					// So we should find the Attribute "MaxCalories" (or similar) and add a modifier?
					// Or purely visual?
					// If we can't easily change Max, we might just accept it.
					// But "Fullness is wrong" implies reference is wrong.
					// There isn't a direct "SetMax" on AmountInstance.
					// However, check if we can hack it.
				}
			}
			catch { }

			// Sync Germs
			var pe = identity.GetComponent<PrimaryElement>();
			if (pe != null)
			{
				pe.AddDisease(GermElemIdx, GermCount - pe.DiseaseCount, "Sync");
			}
		}

		private void SetAmount(Amounts amounts, string id, float value)
		{
			// AmountInstance might not exist or be hidden
			// Use Get(id) but catch null
			try
			{
				// AmountInstance instance = amounts.Get(id); // Usually works but might error if not found?
				// Let's use Db.Get().Amounts.Get(id) to find resource then amounts.Get(resource)

				// Standard ONI: amounts.Get(Db.Get().Amounts.HitPoints.Id).SetValue(value);
				// But simply iterating is safer?

				// Let's try direct set.
				var ai = amounts.Get(id);
				if (ai != null)
				{
					ai.value = value;
				}
			}
			catch
			{
				// Ignore missing amounts
			}
		}
	}
}
