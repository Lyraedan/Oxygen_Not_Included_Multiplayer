using HarmonyLib;
using KSerialization;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Social;
using System.Collections.Generic;

namespace ONI_MP.Patches.Social
{
	// Sync assignments (Minion -> Schedule)
	[HarmonyPatch(typeof(Schedule), "Assign")]
	public static class ScheduleAssignPatch
	{
		public static void Postfix(Schedule __instance, Schedulable schedulable)
		{
			if (!MultiplayerSession.InSession) return;
			if (ScheduleAssignmentPacket.IsApplying) return;

            int netId = schedulable.GetNetId();
            if (netId != 0)
			{
                List<Schedule> schedules = ScheduleManager.Instance.schedules;
                if (schedules == null)
					return;

                int index = schedules.IndexOf(__instance);
                if (index != -1)
				{
                    var packet = new ScheduleAssignmentPacket
					{
						NetId = netId,
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
