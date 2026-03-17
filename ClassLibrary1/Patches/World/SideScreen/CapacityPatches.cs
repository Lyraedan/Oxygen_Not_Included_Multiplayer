using HarmonyLib;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Profiling;
using UnityEngine;

namespace ONI_MP.Patches.World.SideScreen
{
	/// <summary>
	/// Patches for capacity control side screens (batteries, gas reservoirs, etc.)
	/// </summary>

	/// <summary>
	/// Sync capacity changes from CapacityControlSideScreen
	/// </summary>
	[HarmonyPatch(typeof(CapacityControlSideScreen), nameof(CapacityControlSideScreen.UpdateMaxCapacity))]
	public static class CapacityControlSideScreen_UpdateMaxCapacity_Patch
	{
		public static void Postfix(CapacityControlSideScreen __instance, float newValue)
		{
			Profiler.Active.Scope();

			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (__instance.target == null) return;

			// Get the GameObject from the IUserControlledCapacity target
			var targetComponent = __instance.target as Component;
			if (targetComponent != null)
			{
				SideScreenSyncHelper.SyncCapacityChange(targetComponent.gameObject, newValue);
			}
		}
	}

	/// <summary>
	/// Register NetworkIdentity when capacity side screen is opened
	/// </summary>
	[HarmonyPatch(typeof(CapacityControlSideScreen), nameof(CapacityControlSideScreen.SetTarget))]
	public static class CapacityControlSideScreen_SetTarget_Patch
	{
		public static void Postfix(CapacityControlSideScreen __instance, GameObject new_target)
		{
			Profiler.Active.Scope();

			if (new_target == null) return;
			var identity = new_target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();
		}
	}
}
