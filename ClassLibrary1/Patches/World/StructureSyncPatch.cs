using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Profiling;

namespace ONI_MP.Patches.World
{
	[HarmonyPatch(typeof(Battery), "OnSpawn")]
	public static class BatterySpawnPatch
	{
		public static void Postfix(Battery __instance)
		{
			Profiler.Active.Scope();

			if (MultiplayerSession.InSession)
			{
				__instance.gameObject.AddOrGet<StructureStateSyncer>();
			}
		}
	}

	[HarmonyPatch(typeof(Generator), "OnSpawn")]
	public static class GeneratorSpawnPatch
	{
		public static void Postfix(Generator __instance)
		{
			Profiler.Active.Scope();

			if (MultiplayerSession.InSession)
			{
				__instance.gameObject.AddOrGet<StructureStateSyncer>();
			}
		}
	}
}
