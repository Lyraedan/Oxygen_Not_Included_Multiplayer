using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Tools.CopySettingsTool;
using UnityEngine;

namespace ONI_MP.Patches.ToolPatches.CopySettings;

[HarmonyPatch(typeof(CopySettingsTool), nameof(CopySettingsTool.OnDragTool))]
public class CopySettingsToolPatch
{
    private static void Postfix(int cell, int distFromOrigin, CopySettingsTool __instance)
    {
        if (!MultiplayerSession.InSession)
            return;

        GameObject gameObject = Traverse.Create(__instance).Field("sourceGameObject").GetValue<GameObject>();
        if (gameObject == null)
            return;

        NetworkIdentity identity = gameObject.GetComponent<NetworkIdentity>();
        if (identity == null)
            return;

        PacketSender.SendToAllOtherPeers(new CopySettingsToolPacket(identity.NetId, cell));
    }
}