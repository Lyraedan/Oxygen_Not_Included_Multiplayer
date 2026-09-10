using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

namespace ONI_Together.Patches.ToolPatches.Deconstruct
{
    [HarmonyPatch(typeof(DeconstructTool), nameof(DeconstructTool.OnDragTool))]
    public static class DeconstructToolPatch
    {
        public static void Postfix(int cell, int distFromOrigin)
        {
            using var _ = Profiler.Scope();
            if (!MultiplayerSession.InActiveSession)
                return;
            DragToolSyncer.RequestDrag("Deconstruct", cell, distFromOrigin);
        }
    }
}
