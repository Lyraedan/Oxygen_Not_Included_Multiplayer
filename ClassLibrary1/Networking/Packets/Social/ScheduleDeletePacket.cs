using HarmonyLib;
using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
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
			if (ScheduleManager.Instance == null) return;

			var schedules = Traverse.Create(ScheduleManager.Instance).Field("schedules").GetValue<List<Schedule>>();
			if (schedules == null) return;

			if (ScheduleIndex >= 0 && ScheduleIndex < schedules.Count)
			{
				var schedule = schedules[ScheduleIndex];

				// Use the game's delete method to ensure cleanup
				// But we need to be careful about loops if we patch DeleteSchedule.
				// We should set a flag.

				ScheduleDeletePacket.IsApplying = true;
				try
				{
					ScheduleManager.Instance.DeleteSchedule(schedule);
					DebugConsole.Log($"[ScheduleDeletePacket] Deleted schedule {ScheduleIndex}");
				}
				finally
				{
					ScheduleDeletePacket.IsApplying = false;
				}
			}
		}

		public static bool IsApplying = false;
	}
}
