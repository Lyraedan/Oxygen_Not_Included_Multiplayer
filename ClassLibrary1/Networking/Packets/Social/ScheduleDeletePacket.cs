using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.Social
{
	public class ScheduleDeletePacket : IPacket
	{
		public int ScheduleIndex;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(ScheduleIndex);
		}

		public void Deserialize(BinaryReader reader)
		{
			ScheduleIndex = reader.ReadInt32();
		}

		public void OnDispatched()
		{
			if (IsApplying)
				return;

			Apply();
		}

		private void Apply()
		{
			if (ScheduleManager.Instance == null) return;

			var schedules = Traverse.Create(ScheduleManager.Instance).Field("schedules").GetValue<List<Schedule>>();
			if (schedules == null) return;

			if (ScheduleIndex >= 0 && ScheduleIndex < schedules.Count)
			{
				var schedule = schedules[ScheduleIndex];

				IsApplying = true;
				try
				{
					ScheduleManager.Instance.DeleteSchedule(schedule);
					DebugConsole.Log($"[ScheduleDeletePacket] Deleted schedule {ScheduleIndex}");
				}
				finally
				{
					IsApplying = false;
				}
			}
		}

		public static bool IsApplying = false;
	}
}
