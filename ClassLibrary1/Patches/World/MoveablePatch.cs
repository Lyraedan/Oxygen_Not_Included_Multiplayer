using HarmonyLib;
using UnityEngine;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Misc;

[HarmonyPatch(typeof(Movable), "OnSpawn")]
public static class MoveablePatch
{
    public static void Postfix(Movable __instance)
    {
        __instance.onPickupComplete += OnPickup;
        DebugConsole.LogNonImportant($"[Movable.OnSpawn] Attached to: {__instance.gameObject.name} ({__instance.GetInstanceID()})");
    }
    static void OnPickup(GameObject go)
    {
        DebugConsole.LogNonImportant($"[Movable.onPickupComplete] Picked up {go.name}");

        if (!MultiplayerSession.IsHost)
            return;

        if (go == null)
            return;

        if (!go.TryGetComponent<NetworkIdentity>(out var identity))
            return;

        DebugConsole.LogNonImportant($"[Movable.onPickupComplete] Picked up NetID {identity.NetId}");

        // Optional: PacketSender.SendToAll(new DespawnPacket { NetId = identity.NetId });
    }
}
