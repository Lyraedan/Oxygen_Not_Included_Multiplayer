using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

namespace ONI_Together.Patches.ToolPatches.Harvest;

[HarmonyPatch(typeof(HarvestTool), nameof(HarvestTool.OnDragTool))]
public class HarvestToolPatch
{
    private static void Postfix(int cell, int distFromOrigin)
    {
        using var _ = Profiler.Scope();
        if (!MultiplayerSession.InActiveSession)
            return;
        DragToolSyncer.RequestDrag("Harvest", cell, distFromOrigin);
    }
}
