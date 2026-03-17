using HarmonyLib;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Profiling;

namespace ONI_MP.Patches.World.SideScreen
{
	/// <summary>
	/// Patches for door state synchronization
	/// </summary>

	/// <summary>
	/// Sync door state changes (Open/Close/Auto)
	/// </summary>
	/// 

	///DO NOT PATCH "Door" DIRECTLY !!

	//[HarmonyPatch(typeof(Door), "QueueStateChange")]
	public static class Door_QueueStateChange_Patch
	{
		public static void Postfix(Door __instance, Door.ControlState nextState)
		{
			Profiler.Active.Scope();

			if (BuildingConfigPacket.IsApplyingPacket) return;
			SideScreenSyncHelper.SyncDoorState(__instance.gameObject, nextState);
		}
		public static void ExecutePatch()
		{
			Profiler.Active.Scope();

			var m_TargetMethod = AccessTools.Method("Door, Assembly-CSharp:QueueStateChange");
			var m_Postfix = AccessTools.Method(typeof(Door_QueueStateChange_Patch), "Postfix");
			MultiplayerMod.Harmony.Patch(m_TargetMethod, null, new HarmonyMethod(m_Postfix));
		}
	}
}
