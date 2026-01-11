using HarmonyLib;
using ONI_MP.Scripts.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Patches.World
{
	internal class Operational_Patches
	{

        [HarmonyPatch(typeof(Operational), nameof(Operational.OnPrefabInit))]
        public class Operational_OnPrefabInit_Patch
		{
            public static void Postfix(Operational __instance)
            {
                __instance.gameObject.AddOrGet<ClientReceiver_Operational>();
            }
        }
	}
}
