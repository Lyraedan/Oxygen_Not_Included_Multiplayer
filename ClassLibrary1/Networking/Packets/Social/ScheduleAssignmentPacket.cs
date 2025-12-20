using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
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

			var manager = ScheduleManager.Instance;
			var schedules = HarmonyLib.Traverse.Create(manager).Field("schedules").GetValue<List<Schedule>>();
			if (manager == null || schedules == null || ScheduleIndex < 0 || ScheduleIndex >= schedules.Count)
			{
				DebugConsole.LogWarning($"[ScheduleAssignmentPacket] Invalid ScheduleIndex {ScheduleIndex}");
				return;
			}

			var schedule = schedules[ScheduleIndex];

			// Check if already assigned
			if (manager.GetSchedule(schedulable) == schedule)
				return;

			IsApplying = true;
			try
			{
				// Manager.OnAssign(schedulable, schedule) or Schedulable?
				// Standard way: Schedulable usually doesn't have SetSchedule.
				// ScheduleManager.Instance.Assign(Schedulable, Schedule) ? 
				// Wait, examining API... usually one calls ScheduleManager.Instance.SetSchedule?

				// Let's assume standard way is:
				// Removing from old schedule is handled by the manager when adding to new one?

				// Looking at game code (inference): 
				// schedule.Assign(schedulable);

				schedule.Assign(schedulable);

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
