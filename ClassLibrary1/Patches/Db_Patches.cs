using HarmonyLib;
using ONI_MP.Patches.World.SideScreen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Profiling;

namespace ONI_MP.Patches
{
	internal class Db_Patches
	{

        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        public class Db_Initialize_Patch
        {
            public static void Postfix(Db __instance)
            {
	            Profiler.Active.Scope();

                Door_QueueStateChange_Patch.ExecutePatch();

			}
        }
	}
}
