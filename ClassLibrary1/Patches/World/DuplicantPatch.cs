﻿using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using UnityEngine;
using System.Linq;
using ONI_MP.Misc;
using ONI_MP.Networking.Components;
using ONI_MP.Networking;

[HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.CreatePrefab))]
public static class DuplicantPatch
{
    public static void Postfix(GameObject __result)
    {
        var saveRoot = __result.GetComponent<SaveLoadRoot>();
        if (saveRoot != null)
            saveRoot.TryDeclareOptionalComponent<NetworkIdentity>();

        if (__result.GetComponent<NetworkIdentity>() == null)
        {
            __result.AddOrGet<NetworkIdentity>();
            DebugConsole.Log("[NetworkIdentity] Injected into Duplicant");
        }

        __result.AddOrGet<EntityPositionHandler>();
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
