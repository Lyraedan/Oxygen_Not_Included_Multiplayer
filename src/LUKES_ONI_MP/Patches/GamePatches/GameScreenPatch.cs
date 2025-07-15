using HarmonyLib;
using ONI_MP.UI;
using UnityEngine;
using static STRINGS.UI.FRONTEND;

namespace ONI_MP.Patches.GamePatches
{
    [HarmonyPatch(typeof(GameScreenManager), "OnSpawn")]
    public static class GameScreenPatch
    {
        static void Postfix(GameScreenManager __instance)
        {
            ChatScreen.Show();
        }
    }

}
