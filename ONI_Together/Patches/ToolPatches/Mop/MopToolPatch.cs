using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

namespace ONI_Together.Patches.ToolPatches.Mop
{
    [HarmonyPatch(typeof(MopTool), "OnDragTool")]
    public static class MoptoolPatch
    {
        public static void Prefix(int cell, int distFromOrigin)
        {
            using var _ = Profiler.Scope();
            if (!MultiplayerSession.InActiveSession)
                return;
            if (!Grid.IsValidCell(cell))
                return;
            DragToolSyncer.RequestDrag("Mop", cell, distFromOrigin);
        }
    }
}
