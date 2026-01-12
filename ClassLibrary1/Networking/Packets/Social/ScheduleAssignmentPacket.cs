using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.Social
{
	public class ScheduleAssignmentPacket : IPacket
	{
		public int NetId;
		public int ScheduleIndex;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(ScheduleIndex);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			ScheduleIndex = reader.ReadInt32();
		}

		public void OnDispatched()
		{
			if (IsApplying)
				return;

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
			if (!NetworkIdentityRegistry.TryGet(NetId, out var identity) || identity == null)
			{
				DebugConsole.LogWarning($"[ScheduleAssignmentPacket] NetId {NetId} not found or identity is null.");
				return;
			}

			var schedulable = identity.GetComponent<Schedulable>();
			if (schedulable == null)
			{
				DebugConsole.LogWarning($"[ScheduleAssignmentPacket] NetId {NetId} is not Schedulable.");
				return;
			}

			List<Schedule> schedules = ScheduleManager.Instance.schedules;
			if (schedules == null || ScheduleIndex < 0 || ScheduleIndex >= schedules.Count)
			{
				DebugConsole.LogWarning($"[ScheduleAssignmentPacket] Invalid ScheduleIndex {ScheduleIndex}");
				return;
			}

			Schedule previousSchedule = schedulable.GetSchedule();
            Schedule newSchedule = schedules[ScheduleIndex];
            // Check if already assigned
            if (newSchedule.IsAssigned(schedulable))
				return;

			IsApplying = true;
			try
			{
                previousSchedule.Unassign(schedulable); // Unassign from the old schedule
				newSchedule.Assign(schedulable); // Assign to the new schedule

				DebugConsole.Log($"[ScheduleAssignmentPacket] Assigned {identity.name} to Schedule {ScheduleIndex}");
			}
			finally
			{
				IsApplying = false;
			}
		}

		public static bool IsApplying = false;
	}
}
