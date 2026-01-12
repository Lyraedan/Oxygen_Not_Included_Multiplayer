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
		[HarmonyPatch(typeof(Schedule), "SetBlockGroup")]
		public static class SetBlockGroupPatch
		{
			public static void Postfix(Schedule __instance, int idx, ScheduleGroup group)
			{
				if (!MultiplayerSession.InSession) return;
				if (ScheduleBlockUpdatePacket.IsApplying) return;

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
				// TODO Write ScheduleAddPacket
			}
		}

		[HarmonyPatch(typeof(ScheduleManager), "DeleteSchedule")]
		public static class DeleteSchedulePatch
		{
			public static void Prefix(ScheduleManager __instance, Schedule schedule)
			{
				if (!MultiplayerSession.InSession) return;
				if (ScheduleDeletePacket.IsApplying) return;

				int index = schedule.GetScheduleIndex();
				if (index != -1)
				{
					var packet = new ScheduleDeletePacket()
					{
						ScheduleIndex = index
					};

					if (MultiplayerSession.IsHost)
						PacketSender.SendToAllClients(packet);
					else
						PacketSender.SendToHost(packet);
				}
			}
		}
	}
}
