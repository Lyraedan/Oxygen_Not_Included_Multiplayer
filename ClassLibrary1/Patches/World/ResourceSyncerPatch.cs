using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Synchronization;
using Shared.Profiling;

namespace ONI_MP.Patches.World
{
	// Attach ResourceSyncer to the Game or World object
	[HarmonyPatch(typeof(Game), "OnSpawn")]
	public static class GameSpawnPatch
	{
		public static void Postfix(Game __instance)
		{
			Profiler.Scope();

			if (MultiplayerSession.IsHost)
			{
				// Attach to Game.Instance.gameObject (Global helper)
				var syncer = __instance.gameObject.GetComponent<ResourceSyncer>();
				if (syncer == null)
				{
					__instance.gameObject.AddComponent<ResourceSyncer>();
				}
			}
			else
			{
				// Client: Clear stale resources
				ResourceSyncer.ClientResources.Clear();
			}
		}
	}
}
