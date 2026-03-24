using HarmonyLib;
using ONI_MP.Networking.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Profiling;

namespace ONI_MP.Patches.World.Buildings
{
	internal class BuildingComplete_Patches
	{

        [HarmonyPatch(typeof(BuildingComplete), nameof(BuildingComplete.OnPrefabInit))]
        public class BuildingComplete_OnPrefabInit_Patch
        {
            public static void Postfix(BuildingComplete __instance)
            {
                using var _ = Profiler.Scope();

                __instance.gameObject.AddOrGet<NetworkIdentity>();
            }
        }
	}
}
