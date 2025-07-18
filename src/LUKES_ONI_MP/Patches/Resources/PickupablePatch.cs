using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Resources;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Patches.Resources
{
    /// <summary>
    /// Patches Pickupable component to synchronize pickup/drop actions and prevent double resources.
    /// </summary>
    public class PickupablePatch
    {
        [HarmonyPatch(typeof(Pickupable), nameof(Pickupable.OnPrefabInit))]
        public static class Pickupable_OnPrefabInit_Patch
        {
            public static void Postfix(Pickupable __instance)
            {
                // Ensure all pickupables have NetworkIdentity for synchronization
                var identity = __instance.GetComponent<NetworkIdentity>();
                if (identity == null)
                {
                    identity = __instance.gameObject.AddComponent<NetworkIdentity>();
                    identity.RegisterIdentity();
                }
            }
        }

        [HarmonyPatch(typeof(Pickupable), "OnPickedUp")]
        public static class Pickupable_OnPickedUp_Patch
        {
            public static void Postfix(Pickupable __instance, Storage storage)
            {
                if (!MultiplayerSession.IsHost) return;
                
                var identity = __instance.GetComponent<NetworkIdentity>();
                var storageIdentity = storage?.GetComponent<NetworkIdentity>();
                var duplicantIdentity = storage?.GetComponent<MinionIdentity>()?.GetComponent<NetworkIdentity>();
                
                if (identity == null) return;

                try
                {
                    var targetNetId = storageIdentity?.NetId ?? duplicantIdentity?.NetId ?? -1;
                    var actorNetId = duplicantIdentity?.NetId ?? -1;
                    
                    var packet = new PickupableActionPacket(
                        MultiplayerSession.LocalSteamID,
                        identity.NetId,
                        actorNetId,
                        targetNetId,
                        PickupableActionPacket.PickupAction.Pickup,
                        __instance.transform.position
                    );
                    
                    PacketSender.SendToAllClients(packet);
                    DebugConsole.Log($"[PickupablePatch] Synced pickup of {identity.NetId} by actor {actorNetId}");
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[PickupablePatch] Error syncing pickup: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(Pickupable), "OnDropped")]
        public static class Pickupable_OnDropped_Patch
        {
            public static void Postfix(Pickupable __instance)
            {
                if (!MultiplayerSession.IsHost) return;
                
                var identity = __instance.GetComponent<NetworkIdentity>();
                if (identity == null) return;

                try
                {
                    var packet = new PickupableActionPacket(
                        MultiplayerSession.LocalSteamID,
                        identity.NetId,
                        -1, // No specific actor for drops
                        -1, // No target
                        PickupableActionPacket.PickupAction.Drop,
                        __instance.transform.position
                    );
                    
                    PacketSender.SendToAllClients(packet);
                    DebugConsole.Log($"[PickupablePatch] Synced drop of {identity.NetId} at {__instance.transform.position}");
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[PickupablePatch] Error syncing drop: {ex.Message}");
                }
            }
        }

        // Patch to prevent double resource spawning from digging
        [HarmonyPatch(typeof(Pickupable), nameof(Pickupable.TryAbsorb))]
        public static class Pickupable_TryAbsorb_Patch
        {
            public static bool Prefix(Pickupable __instance, Pickupable other, bool hide_effects, bool allow_mass_change)
            {
                // Only allow host to process absorb operations
                if (!MultiplayerSession.IsHost)
                {
                    return false;
                }
                
                return true;
            }
            
            public static void Postfix(Pickupable __instance, Pickupable other, bool hide_effects, bool allow_mass_change, bool __result)
            {
                if (!MultiplayerSession.IsHost || !__result) return;
                
                var identity = __instance.GetComponent<NetworkIdentity>();
                var otherIdentity = other?.GetComponent<NetworkIdentity>();
                
                if (identity == null) return;

                try
                {
                    // Notify that the "other" pickupable was absorbed/consumed
                    if (otherIdentity != null)
                    {
                        var packet = new PickupableActionPacket(
                            MultiplayerSession.LocalSteamID,
                            otherIdentity.NetId,
                            identity.NetId,
                            -1,
                            PickupableActionPacket.PickupAction.Consume,
                            other.transform.position
                        );
                        
                        PacketSender.SendToAllClients(packet);
                        DebugConsole.Log($"[PickupablePatch] Synced absorption/consumption of {otherIdentity.NetId} by {identity.NetId}");
                    }
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[PickupablePatch] Error syncing absorb: {ex.Message}");
                }
            }
        }

        // Prevent clients from picking up items that are already reserved
        [HarmonyPatch(typeof(Pickupable), nameof(Pickupable.CouldBePickedUpByMinion))]
        public static class Pickupable_CouldBePickedUpByMinion_Patch
        {
            public static void Postfix(Pickupable __instance, GameObject carrier, ref bool __result)
            {
                if (__result && !MultiplayerSession.IsHost)
                {
                    // For clients, check if the item is marked as reserved/stored to prevent pickup conflicts
                    if (__instance.HasTag(GameTags.Stored))
                    {
                        __result = false;
                        // DebugConsole.Log($"[PickupablePatch] Prevented client pickup of reserved item");
                    }
                }
            }
        }
    }
}
