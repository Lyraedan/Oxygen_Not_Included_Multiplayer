using System.Collections;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Scripts;
using UnityEngine;

[HarmonyPatch(typeof(BaseMinionConfig), nameof(BaseMinionConfig.BaseMinion))]
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

[HarmonyPatch(typeof(BaseMinionConfig), nameof(BaseMinionConfig.BaseOnSpawn))]
public static class DuplicantSpawnPatch
{
	public static void Postfix(GameObject go)
	{
		go.AddOrGet<MinionMultiplayerInitializer>();
	}
}
