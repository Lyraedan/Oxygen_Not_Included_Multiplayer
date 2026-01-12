using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.Social
{
	public class ScheduleUpdatePacket : IPacket
	{
		public int ScheduleIndex;
		public string Name;
		public bool Alarm;
		public List<string> Blocks = new List<string>();

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(ScheduleIndex);
			writer.Write(Name ?? string.Empty);
			writer.Write(Alarm);
			writer.Write(Blocks.Count);
			foreach (var block in Blocks)
			{
				writer.Write(block ?? string.Empty);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			ScheduleIndex = reader.ReadInt32();
			Name = reader.ReadString();
			Alarm = reader.ReadBoolean();
			int count = reader.ReadInt32();
			Blocks = new List<string>(count);
			for (int i = 0; i < count; i++)
			{
				Blocks.Add(reader.ReadString());
			}
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
			{
				Apply();
				PacketSender.SendToAllClients(this);
			}
			else
			{
				Apply();
			}
		}

		private void Apply()
		{
			// Ensure schedule exists or add new
			// The game handles adding via ScheduleManager.AddSchedule
			// We need to manage the list size to match Index.
			// If Index >= Count, we need to add schedules.

			var schedules = ScheduleManager.Instance.schedules;
			if (schedules == null) return;

			while (schedules.Count <= ScheduleIndex)
			{
				var defaultGroups = Db.Get().ScheduleGroups.allGroups;
				if (schedules.Count > 0)
				{
					defaultGroups = HarmonyLib.Traverse.Create(schedules[0]).Field("blockGroups").GetValue<List<ScheduleGroup>>() ?? defaultGroups;
				}
                ScheduleManager.Instance.AddSchedule(defaultGroups, "Synced Schedule", false);
			}

			var schedule = schedules[ScheduleIndex];

			IsApplying = true;
			try
			{
				// Update Name
				if (schedule.name != Name)
				{
					schedule.name = Name;
					// Trigger name change event? ScheduleManager usually handles UI updates via events.
				}

				// Update Alarm
				if (schedule.alarmActivated != Alarm)
				{
					schedule.alarmActivated = Alarm;
				}

				// Update Blocks
				// Schedule.SetBlockGroup(int index, ScheduleGroup group)
				// Blocks list should be 24 long?
				// Let's assume packet sends full 24 blocks.

				var groups = Db.Get().ScheduleGroups;

				var blocks = schedule.blocks;
				//var blockGroups = HarmonyLib.Traverse.Create(schedule).Field("blockGroups").GetValue<List<ScheduleGroup>>();
				//if (blockGroups == null) return;

				for (int i = 0; i < Blocks.Count && i < blocks.Count; i++)
				{
					string groupId = Blocks[i];
					if (blocks[i].GroupId != groupId)
					{
						// Find the group resource
						var group = groups.resources.Find(g => g.Id == groupId);
						if (group != null)
						{
							schedule.SetBlockGroup(i, group);
						}
					}
				}

				DebugConsole.Log($"[ScheduleUpdatePacket] Updated schedule {ScheduleIndex}: {Name}");
			}
			finally
			{
				IsApplying = false;
			}
		}

		public static bool IsApplying = false;
	}
}
