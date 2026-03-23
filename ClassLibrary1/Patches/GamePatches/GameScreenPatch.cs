using HarmonyLib;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.UI;
using Shared.Profiling;
using UnityEngine;

namespace ONI_MP.Patches.GamePatches
{
	[HarmonyPatch(typeof(GameScreenManager), "OnSpawn")]
	public static class GameScreenPatch
	{
		static void Postfix(GameScreenManager __instance)
		{
			Profiler.Scope();

			// Setup indicators
			NetworkIndicatorsScreen.Show();

			// Setup chat window
            ChatScreen.Show();
		}
	}

}
