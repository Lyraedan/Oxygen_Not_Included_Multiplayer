using ONI_MP.Api.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	public class PrioritizeStatePacket : IPacket
	{
		public struct PriorityData
		{
			public int NetId;
			public int PriorityClass;
			public int PriorityValue;
		}

		public List<PriorityData> Priorities = new List<PriorityData>();
		public static bool IsApplying = false;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Priorities.Count);
			foreach (var p in Priorities)
			{
				writer.Write(p.NetId);
				writer.Write(p.PriorityClass);
				writer.Write(p.PriorityValue);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			Priorities = new List<PriorityData>(count);
			for (int i = 0; i < count; i++)
			{
				Priorities.Add(new PriorityData
				{
					NetId = reader.ReadInt32(),
					PriorityClass = reader.ReadInt32(),
					PriorityValue = reader.ReadInt32()
				});
			}
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost) return;

			try
			{
				IsApplying = true;
				foreach (var p in Priorities)
				{
					if (NetworkIdentityRegistry.TryGet(p.NetId, out var identity) && identity != null)
					{
						var prioritizable = identity.GetComponent<Prioritizable>();
						if (prioritizable != null)
						{
							var newSetting = new PrioritySetting((PriorityScreen.PriorityClass)p.PriorityClass, p.PriorityValue);
							// Only update if different to avoid event spam?
							// GetMasterPriority() returns PrioritySetting.
							if (!prioritizable.GetMasterPriority().Equals(newSetting))
							{
								prioritizable.SetMasterPriority(newSetting);
							}
						}
					}
				}
			}
			finally
			{
				IsApplying = false;
			}
		}
	}
}
