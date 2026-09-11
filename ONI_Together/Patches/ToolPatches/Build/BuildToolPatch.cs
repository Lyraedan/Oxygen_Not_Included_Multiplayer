using System;
using System.Linq;
using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;
using UnityEngine;

namespace ONI_Together.Patches.ToolPatches.Build
{
    [HarmonyPatch(typeof(BuildTool), nameof(BuildTool.TryBuild))]
    public static class BuildToolPatch
    {
        static void Postfix(BuildTool __instance, int cell)
        {
            using var _ = Profiler.Scope();

            if (!MultiplayerSession.InActiveSession || __instance == null)
                return;

            var def = __instance.def;
            var selectedElements = __instance.selectedElements;
            var orientation = __instance.GetBuildingOrientation;

            if (def == null || selectedElements == null)
                return;

            bool instantBuild = DebugHandler.InstantBuildMode || (Game.Instance.SandboxModeActive && SandboxToolParameterMenu.instance.settings.InstantBuild);
            var tags = selectedElements.Select(t => t.ToString()).ToList();
            BuildToolSyncer.RequestBuild(def.PrefabID, cell, (int)orientation, tags, (int)def.ObjectLayer, instantBuild);
        }
    }
}