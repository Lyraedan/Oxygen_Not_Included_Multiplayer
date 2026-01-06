using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Tools.Capture;
using UnityEngine;

namespace ONI_MP.Patches.ToolPatches.Capture;

[HarmonyPatch(typeof(CaptureTool), nameof(CaptureTool.OnDragComplete))]
public class CaptureToolPatch
{
    private static void Postfix(Vector3 downPos, Vector3 upPos, CaptureTool __instance)
    {
        if (!MultiplayerSession.InSession)
            return;

        Traverse get_regularized_pos = Traverse.Create(__instance).Method("GetRegularizedPos", [typeof(Vector2), typeof(bool)]);

        object min_object = get_regularized_pos?.GetValue(Vector2.Min(downPos, upPos), true);
        object max_object = get_regularized_pos?.GetValue(Vector2.Max(downPos, upPos), false);

        if (min_object == null || max_object == null)
            return;

        PacketSender.SendToAllOtherPeers(new CaptureToolPacket((Vector2)min_object, (Vector2)max_object));
    }
}