using HarmonyLib;
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
    /// Sync enabled state changes from side menu toggle
    /// </summary>
    [HarmonyPatch(typeof(BuildingEnabledButton), "OnMenuToggle")]
    public static class Building_EnableStateChange_Patch
    {
        public static void Postfix(BuildingEnabledButton __instance)
        {
            if (BuildingConfigPacket.IsApplyingPacket) return;
            SideScreenSyncHelper.SyncBuildingEnabledStateChange(__instance.gameObject, __instance.queuedToggle);
        }
    }
}
