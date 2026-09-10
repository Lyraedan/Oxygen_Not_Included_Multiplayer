using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

[HarmonyPatch(typeof(DisinfectTool), "OnDragTool")]
public class DisinfectToolPatch
{
    [HarmonyPrefix]
    public static void Prefix(int cell, int distFromOrigin)
    {
        using var _ = Profiler.Scope();
        if (!MultiplayerSession.InActiveSession)
            return;
        DragToolSyncer.RequestDrag("Disinfect", cell, distFromOrigin);
    }
}
