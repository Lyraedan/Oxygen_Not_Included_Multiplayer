using HarmonyLib;
using ONI_MP.Networking;
using UnityEngine;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch(typeof(BatteryTracker), "UpdateData")]
    public static class BatteryTrackerPatch
    {
        public static bool Prefix(BatteryTracker __instance)
        {
            // Singleplayer
            if(!MultiplayerSession.InSession)
            {
                return true;
            }

            return MultiplayerSession.IsHost; // Block clients from executing this (For some reason it causes crashes at hard syncs?)
        }
    }
}
