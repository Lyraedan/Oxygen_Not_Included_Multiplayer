using HarmonyLib;
using ONI_MP.Networking.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Patches.World
{
	internal class WorkablePatch
	{

		[HarmonyPatch(typeof(Workable), nameof(Workable.OnSpawn))]
		public class Workable_OnSpawn_Patch
		{
			public static void Postfix(Workable __instance)
			{
				//if (__instance.multitoolContext.IsValid && __instance.multitoolHitEffectTag.IsValid)
				__instance.gameObject.AddOrGet<NetworkIdentity>();
			}
		}
	}
}
