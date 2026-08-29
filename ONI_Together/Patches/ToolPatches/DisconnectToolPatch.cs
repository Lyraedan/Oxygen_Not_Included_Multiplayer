using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;
using UnityEngine;

namespace ONI_Together.Patches.ToolPatches
{
    internal class DisconnectToolPatch
    {
        [HarmonyPatch(typeof(DisconnectTool), nameof(DisconnectTool.OnDragComplete))]
        public class DisconnectTool_OnDragComplete_Patch
        {
            public static void Prefix(DisconnectTool __instance, Vector3 downPos, Vector3 upPos)
            {
                using var _ = Profiler.Scope();
                if (!MultiplayerSession.InActiveSession)
                    return;
                if (__instance.singleDisconnectMode)
                    upPos = __instance.SnapToLine(upPos);
                DragToolSyncer.RequestDragComplete("Disconnect", downPos, upPos);
            }
        }
    }
}
