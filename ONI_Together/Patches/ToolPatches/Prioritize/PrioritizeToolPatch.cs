using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

[HarmonyPatch(typeof(PrioritizeTool), nameof(PrioritizeTool.OnDragTool))]
public static class PrioritizeToolPatch
{
    public static void Postfix(int cell, int distFromOrigin)
    {
        using var _ = Profiler.Scope();
        if (!MultiplayerSession.InActiveSession)
            return;
        DragToolSyncer.RequestDrag("Prioritize", cell, distFromOrigin);
    }
}
