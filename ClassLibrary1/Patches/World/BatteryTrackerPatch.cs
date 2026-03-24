using HarmonyLib;
using ONI_MP.Networking;
using Shared.Profiling;

namespace ONI_MP.Patches.World
{
	[HarmonyPatch(typeof(BatteryTracker), "UpdateData")]
	public static class BatteryTrackerPatch
	{
		public static bool Prefix(BatteryTracker __instance)
		{
			Profiler.Scope();

			if (GameClient.IsHardSyncInProgress)
				return false;

			// Singleplayer
			if (!MultiplayerSession.InSession)
			{
				return true;
			}

			return MultiplayerSession.IsHost; // Block clients from executing this (For some reason it causes crashes at hard syncs?)
		}
	}
}
