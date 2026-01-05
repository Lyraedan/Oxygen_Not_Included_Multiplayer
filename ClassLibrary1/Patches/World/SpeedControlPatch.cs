using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Patches
{
	[HarmonyPatch(typeof(SpeedControlScreen))]
	public static class SpeedControlScreen_SendSpeedPacketPatch
	{
		public static bool IsSyncing = false;

		[HarmonyPatch("SetSpeed")]
		[HarmonyPostfix]
		public static void SetSpeed_Postfix(int Speed)
		{
			if (IsSyncing) return;

			var packet = new SpeedChangePacket((SpeedChangePacket.SpeedState)Speed);

			PacketSender.SendToAllOtherPeers(packet);
			DebugConsole.Log($"[SpeedControl] Sent SpeedChangePacket: {packet.Speed}");
		}

		[HarmonyPatch("TogglePause")]
		[HarmonyPostfix]
		public static void TogglePause_Postfix(SpeedControlScreen __instance)
		{
			if (IsSyncing) return;

			var speedState = __instance.IsPaused
					? SpeedChangePacket.SpeedState.Paused
					: (SpeedChangePacket.SpeedState)__instance.GetSpeed();

			var packet = new SpeedChangePacket(speedState);
            PacketSender.SendToAllOtherPeers(packet);
            DebugConsole.Log($"[SpeedControl] Sent SpeedChangePacket (pause toggle): {packet.Speed}");
		}
	}
}
