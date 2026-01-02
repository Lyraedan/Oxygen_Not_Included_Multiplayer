using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Patches.World.SideScreen
{
    /// <summary>
    /// Patches for building enabled/disabled state synchronization
    /// </summary>

    /// <summary>
    /// Sync queueing of enabled state change from side menu toggle
    /// </summary>
    [HarmonyPatch(typeof(BuildingEnabledButton), "OnMenuToggle")]
    public static class Building_QueueEnableStateChange_Patch
    {
        public static void Postfix(BuildingEnabledButton __instance)
        {
            if (!MultiplayerSession.InSession) return;

            SideScreenSyncHelper.SyncQueueBuildingEnabledToggle(__instance.gameObject, __instance.queuedToggle);
        }
    }

    /// <summary>
    /// Sync actual change of building enabled state
    /// </summary>
    [HarmonyPatch(typeof(BuildingEnabledButton), "HandleToggle")]
    public static class Building_HandleEnableStateChange_Patch
    {
        public static void Postfix(BuildingEnabledButton __instance)
        {
            if (!MultiplayerSession.InSession) return;
            if (!MultiplayerSession.IsHost) return; // Only the host building enabled changes matter

            SideScreenSyncHelper.SyncBuildingEnabledChange(__instance.gameObject, __instance.buildingEnabled);
        }
    }
}
