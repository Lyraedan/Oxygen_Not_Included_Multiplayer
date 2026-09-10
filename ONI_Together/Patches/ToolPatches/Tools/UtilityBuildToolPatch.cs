using System.Linq;
using HarmonyLib;
using ONI_Together.Misc;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

namespace ONI_Together.Patches.ToolPatches.Build
{
    [HarmonyPatch(typeof(BaseUtilityBuildTool), nameof(BaseUtilityBuildTool.BuildPath))]
    public static class UtilityBuildToolPatch
    {
        public static void Prefix(BaseUtilityBuildTool __instance)
        {
            using var _ = Profiler.Scope();
            if (!MultiplayerSession.InActiveSession) return;
            if (__instance.path == null || __instance.def == null || __instance.path.Count == 0) return;
            bool instantBuild = DebugHandler.InstantBuildMode || (Game.Instance.SandboxModeActive && SandboxToolParameterMenu.instance.settings.InstantBuild);
            var chunks = BuildingUtils.EncodeUtilityPathWithValidity(__instance.path);
            var mats = __instance.selectedElements.Select(t => t.ToString()).ToList();
            UtilityBuildToolSyncer.RequestUtilityBuild(__instance.def.PrefabID, mats, chunks, __instance.facadeID, instantBuild);
        }
    }
}
