using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Resources;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Patches.Resources
{
    /// <summary>
    /// Patches Storage component to synchronize container contents in real-time
    /// instead of relying only on hard sync.
    /// </summary>
    public class StoragePatch
    {
        private static float _lastSyncTime = 0f;
        private const float SYNC_INTERVAL = 2f; // Sync every 2 seconds to avoid spam

        [HarmonyPatch(typeof(Storage), nameof(Storage.Store))]
        public static class Storage_Store_Patch
        {
            public static void Postfix(Storage __instance, GameObject go, bool hide_popups, bool block_tags, bool do_disease_transfer, bool do_notify_stored)
            {
                if (!MultiplayerSession.IsHost) return;
                
                // Get network identity
                var identity = __instance.GetComponent<NetworkIdentity>();
                if (identity == null) return;

                // Throttle sync updates
                if (Time.time - _lastSyncTime < SYNC_INTERVAL) return;
                _lastSyncTime = Time.time;

                try
                {
                    var packet = new StorageUpdatePacket(MultiplayerSession.LocalSteamID, identity.NetId, __instance);
                    PacketSender.SendToAllClients(packet);
                    
                    DebugConsole.Log($"[StoragePatch] Synced storage {identity.NetId} after storing item");
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[StoragePatch] Error syncing storage after store: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(Storage), nameof(Storage.Drop))]
        public static class Storage_Drop_Patch
        {
            public static void Postfix(Storage __instance, GameObject go, bool do_copy_over_tags)
            {
                if (!MultiplayerSession.IsHost) return;
                
                // Get network identity
                var identity = __instance.GetComponent<NetworkIdentity>();
                if (identity == null) return;

                // Throttle sync updates
                if (Time.time - _lastSyncTime < SYNC_INTERVAL) return;
                _lastSyncTime = Time.time;

                try
                {
                    var packet = new StorageUpdatePacket(MultiplayerSession.LocalSteamID, identity.NetId, __instance);
                    PacketSender.SendToAllClients(packet);
                    
                    DebugConsole.Log($"[StoragePatch] Synced storage {identity.NetId} after dropping item");
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[StoragePatch] Error syncing storage after drop: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(Storage), nameof(Storage.DropAll))]
        public static class Storage_DropAll_Patch
        {
            public static void Postfix(Storage __instance, bool vent_gas, bool dump_liquid, Vector3 offset, bool do_copy_over_tags)
            {
                if (!MultiplayerSession.IsHost) return;
                
                // Get network identity
                var identity = __instance.GetComponent<NetworkIdentity>();
                if (identity == null) return;

                try
                {
                    var packet = new StorageUpdatePacket(MultiplayerSession.LocalSteamID, identity.NetId, __instance);
                    PacketSender.SendToAllClients(packet);
                    
                    DebugConsole.Log($"[StoragePatch] Synced storage {identity.NetId} after dropping all items");
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[StoragePatch] Error syncing storage after drop all: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(Storage), nameof(Storage.ConsumeIgnoringDisease))]
        public static class Storage_ConsumeIgnoringDisease_Patch
        {
            public static void Postfix(Storage __instance, Tag tag, float amount)
            {
                if (!MultiplayerSession.IsHost) return;
                
                // Get network identity
                var identity = __instance.GetComponent<NetworkIdentity>();
                if (identity == null) return;

                // Throttle sync updates
                if (Time.time - _lastSyncTime < SYNC_INTERVAL) return;
                _lastSyncTime = Time.time;

                try
                {
                    var packet = new StorageUpdatePacket(MultiplayerSession.LocalSteamID, identity.NetId, __instance);
                    PacketSender.SendToAllClients(packet);
                    
                    DebugConsole.Log($"[StoragePatch] Synced storage {identity.NetId} after consuming {amount} {tag}");
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[StoragePatch] Error syncing storage after consume: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(Storage), nameof(Storage.AddOre))]
        public static class Storage_AddOre_Patch
        {
            public static void Postfix(Storage __instance, SimHashes element, float mass, float temperature, byte disease_idx, int disease_count)
            {
                if (!MultiplayerSession.IsHost) return;
                
                // Get network identity
                var identity = __instance.GetComponent<NetworkIdentity>();
                if (identity == null) return;

                // Throttle sync updates
                if (Time.time - _lastSyncTime < SYNC_INTERVAL) return;
                _lastSyncTime = Time.time;

                try
                {
                    var packet = new StorageUpdatePacket(MultiplayerSession.LocalSteamID, identity.NetId, __instance);
                    PacketSender.SendToAllClients(packet);
                    
                    DebugConsole.Log($"[StoragePatch] Synced storage {identity.NetId} after adding {mass} ore");
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[StoragePatch] Error syncing storage after add ore: {ex.Message}");
                }
            }
        }
    }
}
