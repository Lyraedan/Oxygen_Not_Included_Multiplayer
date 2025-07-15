using HarmonyLib;
using UnityEngine;
using System.Linq;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.DuplicantActions;
using System.Diagnostics.Tracing;

[HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.CreatePrefab))]
public static class DuplicantPatch
{
    public static void Postfix(GameObject __result)
    {
        var saveRoot = __result.GetComponent<SaveLoadRoot>();
        if (saveRoot != null)
            saveRoot.TryDeclareOptionalComponent<NetworkIdentity>();

        var networkIdentity = __result.GetComponent<NetworkIdentity>();
        if (networkIdentity == null)
        {
            networkIdentity = __result.AddOrGet<NetworkIdentity>();
            DebugConsole.Log("[NetworkIdentity] Injected into Duplicant");
        }

        __result.AddOrGet<EntityPositionHandler>();
        __result.AddOrGet<ConditionTracker>();
    }

    public static void ToggleEffect(GameObject minion, string eventName, string context, bool enable)
    {
        if (!MultiplayerSession.InSession || MultiplayerSession.IsClient)
            return;

        if (!minion.TryGetComponent(out NetworkIdentity net))
        {
            DebugConsole.LogWarning("[ToggleEffect] Minion is missing NetworkIdentity");
            return;
        }

        var packet = new ToggleMinionEffectPacket
        {
            NetId = net.NetId,
            Enable = enable,
            Context = context,
            Event = eventName
        };

        PacketSender.SendToAllClients(packet);
    }
}

[HarmonyPatch(typeof(MinionConfig), "OnSpawn")]
public static class DuplicantToolSyncPatch
{
    public static void Postfix(GameObject go)
    {
        if (!go.HasTag(GameTags.Minions.Models.Standard) || go.HasTag(GameTags.Minions.Models.Bionic)) return;

        var identity = go.GetComponent<NetworkIdentity>();
        if (identity == null) return;

        /*
        foreach (var toggler in go.GetComponentsInChildren<KBatchedAnimEventToggler>(true))
        {
            if (toggler == null || toggler.entries == null || toggler.entries.Count == 0)
                continue;

            string enableEvent = toggler.enableEvent;
            string disableEvent = toggler.disableEvent;
            string prefabName = toggler.entries[0].controller?.name ?? "";
            string animName = toggler.entries[0].anim ?? "";

            if (string.IsNullOrEmpty(prefabName)) continue;

            if (enableEvent != "LaserOn" &&
                enableEvent != "BuildToolOn" &&
                enableEvent != "MopOn")
                continue;

            if (toggler.eventSource == null) continue;

            toggler.Subscribe(toggler.eventSource, Hash.SDBMLower(enableEvent), _ =>
            {
                //DebugConsole.Log($"[ToolEquip] {enableEvent} -> Equip");
                if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost)
                    return;

                PacketSender.SendToAllClients(new ToolEquipPacket
                {
                    TargetNetId = identity.NetId,
                    PrefabName = prefabName,
                    ParentBoneName = "snapto_hand",
                    AnimName = animName,
                    LoopAnim = true,
                    Equip = true
                });
            });

            toggler.Subscribe(toggler.eventSource, Hash.SDBMLower(disableEvent), _ =>
            {
                //DebugConsole.Log($"[ToolEquip] {disableEvent} -> Unequip");
                if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost)
                    return;

                PacketSender.SendToAllClients(new ToolEquipPacket
                {
                    TargetNetId = identity.NetId,
                    Equip = false
                });
            });
        }*/
    }
}

