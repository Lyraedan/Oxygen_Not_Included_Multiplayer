using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.Components;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;
using UnityEngine;

namespace ONI_Together.Patches.ToolPatches.CopySettings;

[HarmonyPatch(typeof(CopySettingsTool), nameof(CopySettingsTool.OnDragTool))]
public class CopySettingsToolPatch
{
    private static void Postfix(int cell, CopySettingsTool __instance)
    {
        using var _ = Profiler.Scope();

        if (!MultiplayerSession.InActiveSession)
            return;

        GameObject src = __instance.sourceGameObject;
        if (src == null)
            return;

        NetworkIdentity identity = src.GetComponent<NetworkIdentity>();
        if (identity == null)
            return;

        CopySettingsToolSyncer.RequestCopy(identity.NetId, cell);
    }
}