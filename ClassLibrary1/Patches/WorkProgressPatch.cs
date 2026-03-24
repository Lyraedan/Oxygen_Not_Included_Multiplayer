using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using System.Collections.Generic;
using Shared.Profiling;
using UnityEngine;

[HarmonyPatch(typeof(Workable), nameof(Workable.WorkTick))]
public static class WorkProgressPatch
{
	private static Dictionary<int, float> nextSendTime = new Dictionary<int, float>();
	private const float SEND_INTERVAL = 0.5f;

	public static void Postfix(Workable __instance)
	{
		Profiler.Scope();

		if (!MultiplayerSession.IsHost || !MultiplayerSession.InSession)
			return;

		if (__instance.IsNullOrDestroyed())
			return;

		if (__instance is not Constructable && __instance is not Deconstructable)
			return;

		float workTime = __instance.GetWorkTime();
		if (workTime <= 0f || float.IsInfinity(workTime))
			return;

		if (__instance.GetPercentComplete() < 0f)
			return;

		int cell = Grid.PosToCell(__instance.transform.position);
		float now = Time.time;

		if (nextSendTime.TryGetValue(cell, out float next) && now < next)
			return;

		nextSendTime[cell] = now + SEND_INTERVAL;
		PacketSender.SendToAllClients(new WorkProgressPacket(cell, __instance.WorkTimeRemaining));
	}

	public static void ClearTracking()
	{
		Profiler.Scope();

		nextSendTime.Clear();
	}
}
