using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static WorkerBase;

namespace ONI_MP.Patches.DuplicantActions
{
	internal class StandardWorker_Patches
	{

		[HarmonyPatch(typeof(StandardWorker), nameof(StandardWorker.StartWork))]
		public class StandardWorker_StartWork_Patch
		{
			public static void Postfix(StandardWorker __instance, StartWorkInfo start_work_info)
			{
				if (__instance.IsNullOrDestroyed()) 
					return;

				if (start_work_info.IsNullOrDestroyed())
					return;

				if (!Utils.IsHostMinion(__instance))
					return;

				///skip deliverables for now
				if (start_work_info.GetType() != typeof(WorkerBase.StartWorkInfo))
					return;

				///Skip bionic's sleeping
				if (start_work_info.workable.GetType() == typeof(DefragmentationZone))
					return;

				PacketSender.SendToAllClients(new StandardWorker_WorkingState_Packet(__instance, start_work_info.workable,true));
			}
		}

		[HarmonyPatch(typeof(StandardWorker), nameof(StandardWorker.StopWork))]
		public class StandardWorker_StopWork_Patch
		{
			public static void Postfix(StandardWorker __instance)
			{
				if (__instance.IsNullOrDestroyed()) 
					return;

				if (!Utils.IsHostMinion(__instance))
					return;
				PacketSender.SendToAllClients(new StandardWorker_WorkingState_Packet(__instance,null, false));
			}
		}
	}
}
