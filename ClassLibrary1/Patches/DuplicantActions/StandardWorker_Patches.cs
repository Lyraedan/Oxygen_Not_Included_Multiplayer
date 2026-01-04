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
                if (!Utils.IsHostMinion(__instance))
                    return;

                if(__instance.animInfo.smi != null && __instance.animInfo.smi is MultitoolController.Instance smi)
                {
                    DebugConsole.Log("Sending multitool packet to clients for " + start_work_info.workable.name);
                    PacketSender.SendToAllClients(new MultiToolSyncPacket(__instance,smi));
                }
            }
        }
	}
}
