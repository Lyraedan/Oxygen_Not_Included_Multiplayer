using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc.World;
using ONI_MP.Networking;
using UnityEngine;
using static STRINGS.UI;

namespace ONI_MP.Patches.GamePatches
{
    // This single class contains BOTH patches.
    public static class GamePatch
    {
        // Patch Game.Update to run the two batchers if host
        [HarmonyPatch(typeof(Game), "Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix()
        {
            if (MultiplayerSession.IsHost)
            {
                InstantiationBatcher.Update();
                WorldUpdateBatcher.Update();
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        [HarmonyPostfix]
        public static void OnSpawnPostfix()
        {

        }
    }
}
