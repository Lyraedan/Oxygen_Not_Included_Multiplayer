using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Tools.Attack;
using UnityEngine;

namespace ONI_MP.Patches.ToolPatches.Attack;

[HarmonyPatch(typeof(AttackTool), nameof(AttackTool.OnDragComplete))]
public class AttackToolPatch
{
    private static void Postfix(Vector3 downPos, Vector3 upPos, AttackTool __instance)
    {
        if (!MultiplayerSession.InSession)
            return;

        Traverse get_regularized_pos = Traverse.Create(__instance).Method("GetRegularizedPos", [typeof(Vector2), typeof(bool)]);

        object min_object = get_regularized_pos?.GetValue(Vector2.Min(downPos, upPos), true);
        object max_object = get_regularized_pos?.GetValue(Vector2.Max(downPos, upPos), false);

        if (min_object == null || max_object == null)
            return;

        AttackToolPacket packet = new AttackToolPacket((Vector2)min_object, (Vector2)max_object);

        if (MultiplayerSession.IsHost)
            PacketSender.SendToAllClients(packet);
        else
            PacketSender.SendToHost(packet);
    }
}