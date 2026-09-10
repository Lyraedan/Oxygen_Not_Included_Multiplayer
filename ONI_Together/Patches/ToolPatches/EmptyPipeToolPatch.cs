using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

namespace ONI_Together.Patches.ToolPatches
{
    internal class EmptyPipeToolPatch
    {
        [HarmonyPatch(typeof(EmptyPipeTool), nameof(EmptyPipeTool.OnDragTool))]
        public class EmptyPipeTool_OnDragTool_Patch
        {
            public static void Postfix(int cell, int distFromOrigin)
            {
                using var _ = Profiler.Scope();
                if (!MultiplayerSession.InActiveSession)
                    return;
                DragToolSyncer.RequestDrag("EmptyPipe", cell, distFromOrigin);
            }
        }
    }
}
