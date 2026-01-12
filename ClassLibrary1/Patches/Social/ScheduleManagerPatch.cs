using HarmonyLib;
using KSerialization;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Social;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Patches.Social
{
	// Sync schedule definitions (name, blocks, alarm)

	public static class SchedulePatch
	{
		// Prevent infinite loops
		private static bool Ignore => ScheduleUpdatePacket.IsApplying;

		[HarmonyPatch(typeof(Schedule), "SetBlockGroup")]
		public static class SetBlockGroupPatch
		{
			public static void Postfix(Schedule __instance, int idx, ScheduleGroup group)
			{
				DebugConsole.Log("SetBlockGroup: " + idx + ", " + group.Name + ", " + group.Id);
				if (!MultiplayerSession.InSession) return;
				//if (Ignore) return;
				//ScheduleSyncHelper.SendUpdate(__instance);

				int scheduleIndex = __instance.GetScheduleIndex();
				// Invalid schedule index
				if (scheduleIndex == -1)
					return;

                ScheduleBlockUpdatePacket packet = new ScheduleBlockUpdatePacket() { 
					ScheduleIndex = scheduleIndex,
					BlockIndex = idx,
					GroupId = group.Id
				};
				if(MultiplayerSession.IsHost)
				{
					PacketSender.SendToAllClients(packet);
				} else
				{
					PacketSender.SendToHost(packet);
				}
				DebugConsole.Log("[SchedulePatch] Set block group update!");
			}
		}

		[HarmonyPatch(typeof(ScheduleManager), "AddSchedule")]
		public static class AddSchedulePatch
		{
			public static void Postfix(Schedule __result)
			{
				DebugConsole.Log("Add schedule!");
				if (!MultiplayerSession.InSession) return;
				// If applying, we ignore
				if (ScheduleUpdatePacket.IsApplying) return;

				ScheduleSyncHelper.SendUpdate(__result);
			}
		}

		[HarmonyPatch(typeof(ScheduleManager), "DeleteSchedule")]
		public static class DeleteSchedulePatch
		{
			public static void Prefix(ScheduleManager __instance, Schedule schedule)
			{
				DebugConsole.Log("Delete schedule");
				if (!MultiplayerSession.InSession) return;
				if (ScheduleDeletePacket.IsApplying) return;

				List<Schedule> schedules = __instance.schedules;
				if (schedules == null) return;

				int index = schedules.IndexOf(schedule);
				if (index != -1)
				{
					var packet = new ScheduleDeletePacket(index);

					if (MultiplayerSession.IsHost)
						PacketSender.SendToAllClients(packet);
					else
						PacketSender.SendToHost(packet);
				}
			}
		}

		// What the fuck
		[HarmonyPatch(typeof(Game), "LateUpdate")]
		public static class ScheduleMonitor
		{
			private static float _timer = 0;
			private static Dictionary<int, int> _hashes = new Dictionary<int, int>();

			public static void Postfix()
			{
				if (!MultiplayerSession.InSession) return;

				_timer += Time.unscaledDeltaTime;
				if (_timer < 1.0f) return; // Check every second
				_timer = 0;

				DebugConsole.Log("What the fuck");
				var schedules = ScheduleManager.Instance.schedules;
				if (schedules == null) return;

				for (int i = 0; i < schedules.Count; i++)
				{
					var schedule = schedules[i];
					if (schedule == null) continue;

					int currentHash = CalculateHash(schedule);

					if (!_hashes.ContainsKey(i))
					{
						_hashes[i] = currentHash;
					}
					else if (_hashes[i] != currentHash)
					{
						// Changed!
						if (!ScheduleUpdatePacket.IsApplying)
						{
							ScheduleSyncHelper.SendUpdate(schedule);
						}
						_hashes[i] = currentHash;
					}
				}
			}

			private static int CalculateHash(Schedule schedule)
			{
				int hash = 17;
				hash = hash * 31 + (schedule.name?.GetHashCode() ?? 0);
				hash = hash * 31 + schedule.alarmActivated.GetHashCode();

				var groups = Traverse.Create(schedule).Field("blockGroups").GetValue<List<ScheduleGroup>>();
				if (groups != null)
				{
					foreach (var g in groups)
					{
						hash = hash * 31 + (g.Id.GetHashCode());
					}
				}
				return hash;
			}
		}

		// Renaming usually happens via the ScheduleScreen, but the Schedule object has name field.
		// There is no SetName method on Schedule.
		// We'll rely on ScheduleManager.OnScheduleChanged if we can find it, or generic update triggers.
		// For now, let's comment out the speculative patches and focus on SetBlockGroup which is the main one.
	}

	public static class ScheduleSyncHelper
	{
		public static void SendUpdate(Schedule schedule)
		{
			if (schedule == null) return;

			var schedules = ScheduleManager.Instance.schedules;
			if (schedules == null) return;

			int index = schedules.IndexOf(schedule);
			if (index == -1) return;

			var blocks = new List<string>();
			var blockGroups = HarmonyLib.Traverse.Create(schedule).Field("blockGroups").GetValue<List<ScheduleGroup>>(); // this is just wrong, blockGroups does not exist
			if (blockGroups == null) return;

			foreach (var block in blockGroups)
			{
				blocks.Add(block.Id);
			}

			var packet = new ScheduleUpdatePacket
			{
				ScheduleIndex = index,
				Name = schedule.name,
				Alarm = schedule.alarmActivated,
				Blocks = blocks
			};

			if (MultiplayerSession.IsHost)
				PacketSender.SendToAllClients(packet);
			else
				PacketSender.SendToHost(packet);

			DebugConsole.Log($"[ScheduleSync] Sent update for Schedule {index}");
		}
	}
}
