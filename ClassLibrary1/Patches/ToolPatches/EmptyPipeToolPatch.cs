using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Tools;
using ONI_MP.Networking.Packets.Tools.Deconstruct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Profiling;

namespace ONI_MP.Patches.ToolPatches
{
	internal class EmptyPipeToolPatch
	{
		[HarmonyPatch(typeof(EmptyPipeTool), nameof(EmptyPipeTool.OnDragTool))]
		public class EmptyPipeTool_OnDragTool_Patch
		{
			public static void Postfix(int cell, int distFromOrigin)
			{
				Profiler.Active.Scope();

				if (!MultiplayerSession.InSession)
					return;

				//prevent recursion
				if (EmptyPipePacket.ProcessingIncoming)
					return;
				PacketSender.SendToAllOtherPeers(new EmptyPipePacket() { cell = cell, distFromOrigin = distFromOrigin });
			}
		}
	}
}
