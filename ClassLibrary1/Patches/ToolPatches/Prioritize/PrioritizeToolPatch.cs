using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Tools;
using ONI_MP.Networking.Packets.Tools.Prioritize;
using System.Collections.Generic;
using ONI_MP.Profiling;
using UnityEngine;

[HarmonyPatch(typeof(PrioritizeTool), nameof(PrioritizeTool.OnDragTool))]
public static class PrioritizeToolPatch
{
	public static void Postfix(int cell, int distFromOrigin)
	{
		Profiler.Active.Scope();

		if (!MultiplayerSession.InSession)
			return;

		//prevent recursion
		if (PrioritizePacket.ProcessingIncoming)
			return;

		PacketSender.SendToAllOtherPeers(new PrioritizePacket { cell = cell, distFromOrigin = distFromOrigin });
	}
}
