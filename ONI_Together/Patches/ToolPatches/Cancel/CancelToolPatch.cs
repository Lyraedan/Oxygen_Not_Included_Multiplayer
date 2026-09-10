using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

namespace ONI_Together.Patches.ToolPatches.Cancel
{
    [HarmonyPatch(typeof(CancelTool), nameof(CancelTool.OnDragTool))]
    public static class CancelToolPatch
    {
        public static void Postfix(int cell, int distFromOrigin)
        {
            using var _ = Profiler.Scope();
            if (!MultiplayerSession.InActiveSession)
                return;
            DragToolSyncer.RequestDrag("Cancel", cell, distFromOrigin);
        }
    }
}
