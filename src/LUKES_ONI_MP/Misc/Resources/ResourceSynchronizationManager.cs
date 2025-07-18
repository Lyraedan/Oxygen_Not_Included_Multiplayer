using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Resources;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;
using Steamworks;

namespace ONI_MP.Misc.Resources
{
    /// <summary>
    /// Manages real-time synchronization of resources and inventory across multiplayer clients.
    /// Coordinates storage updates, inventory sync, and resource transfers to replace hard sync dependency.
    /// </summary>
    public class ResourceSynchronizationManager : MonoBehaviour
    {
        private static ResourceSynchronizationManager _instance;
        public static ResourceSynchronizationManager Instance => _instance;

        private Dictionary<int, float> _lastStorageSync = new Dictionary<int, float>();
        private Dictionary<int, float> _lastInventorySync = new Dictionary<int, float>();
        
        private const float STORAGE_SYNC_INTERVAL = 3f;    // Sync storage every 3 seconds
        private const float INVENTORY_SYNC_INTERVAL = 2f;   // Sync inventory every 2 seconds
        private const float BATCH_SYNC_INTERVAL = 5f;      // Batch sync interval for efficiency

        private float _lastBatchSync = 0f;
        private List<Storage> _storageContainers = new List<Storage>();
        private List<MinionIdentity> _duplicants = new List<MinionIdentity>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                DebugConsole.Log("[ResourceSyncManager] Resource Synchronization Manager initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!MultiplayerSession.IsHost) return;

            // Batch sync at intervals to avoid performance issues
            if (Time.time - _lastBatchSync >= BATCH_SYNC_INTERVAL)
            {
                PerformBatchSync();
                _lastBatchSync = Time.time;
            }
        }

        /// <summary>
        /// Initialize the resource synchronization system
        /// </summary>
        public void Initialize()
        {
            DebugConsole.Log("[ResourceSyncManager] Initializing resource synchronization system");
            
            // Find all existing storage containers and duplicants
            RefreshEntityLists();
            
            DebugConsole.Log($"[ResourceSyncManager] Found {_storageContainers.Count} storage containers and {_duplicants.Count} duplicants");
        }

        /// <summary>
        /// Refresh the lists of storage containers and duplicants to monitor
        /// </summary>
        public void RefreshEntityLists()
        {
            _storageContainers.Clear();
            _duplicants.Clear();

            // Find all storage containers with NetworkIdentity
            var allStorage = FindObjectsOfType<Storage>();
            foreach (var storage in allStorage)
            {
                var identity = storage.GetComponent<NetworkIdentity>();
                if (identity != null)
                {
                    _storageContainers.Add(storage);
                }
            }

            // Find all duplicants
            var allDuplicants = FindObjectsOfType<MinionIdentity>();
            foreach (var duplicant in allDuplicants)
            {
                var identity = duplicant.GetComponent<NetworkIdentity>();
                if (identity != null)
                {
                    _duplicants.Add(duplicant);
                }
            }
        }

        /// <summary>
        /// Perform batch synchronization of all storage containers and duplicant inventories
        /// </summary>
        private void PerformBatchSync()
        {
            // Sync storage containers
            foreach (var storage in _storageContainers)
            {
                if (storage == null) continue;
                
                var identity = storage.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                SyncStorageIfNeeded(storage, identity.NetId);
            }

            // Sync duplicant inventories
            foreach (var duplicant in _duplicants)
            {
                if (duplicant == null) continue;
                
                var identity = duplicant.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                SyncInventoryIfNeeded(duplicant, identity.NetId);
            }

            // Clean up destroyed objects
            _storageContainers.RemoveAll(s => s == null);
            _duplicants.RemoveAll(d => d == null);
        }

        /// <summary>
        /// Sync storage container if enough time has passed since last sync
        /// </summary>
        private void SyncStorageIfNeeded(Storage storage, int netId)
        {
            if (!_lastStorageSync.TryGetValue(netId, out float lastSync))
            {
                lastSync = 0f;
            }

            if (Time.time - lastSync >= STORAGE_SYNC_INTERVAL)
            {
                try
                {
                    var packet = new StorageUpdatePacket(MultiplayerSession.LocalSteamID, netId, storage);
                    PacketSender.SendToAllClients(packet);
                    
                    _lastStorageSync[netId] = Time.time;
                    DebugConsole.Log($"[ResourceSyncManager] Synced storage {netId} ({storage.MassStored():F1}kg)");
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[ResourceSyncManager] Error syncing storage {netId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sync duplicant inventory if enough time has passed since last sync
        /// </summary>
        private void SyncInventoryIfNeeded(MinionIdentity duplicant, int netId)
        {
            if (!_lastInventorySync.TryGetValue(netId, out float lastSync))
            {
                lastSync = 0f;
            }

            if (Time.time - lastSync >= INVENTORY_SYNC_INTERVAL)
            {
                try
                {
                    var packet = new InventorySyncPacket(MultiplayerSession.LocalSteamID, netId);
                    PacketSender.SendToAllClients(packet);
                    
                    _lastInventorySync[netId] = Time.time;
                    DebugConsole.Log($"[ResourceSyncManager] Synced inventory for duplicant {netId}");
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError($"[ResourceSyncManager] Error syncing inventory {netId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Force immediate sync of a specific storage container
        /// </summary>
        public void ForceSyncStorage(int storageNetId)
        {
            if (NetworkIdentityRegistry.TryGet(storageNetId, out NetworkIdentity storageIdentity))
            {
                var storageObj = storageIdentity.gameObject;
                var storage = storageObj.GetComponent<Storage>();
                if (storage != null)
                {
                    try
                    {
                        var packet = new StorageUpdatePacket(MultiplayerSession.LocalSteamID, storageNetId, storage);
                        PacketSender.SendToAllClients(packet);
                        
                        _lastStorageSync[storageNetId] = Time.time;
                        DebugConsole.Log($"[ResourceSyncManager] Force synced storage {storageNetId}");
                    }
                    catch (System.Exception ex)
                    {
                        DebugConsole.LogError($"[ResourceSyncManager] Error force syncing storage {storageNetId}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Force immediate sync of a specific duplicant inventory
        /// </summary>
        public void ForceSyncInventory(int duplicantNetId)
        {
            try
            {
                var packet = new InventorySyncPacket(MultiplayerSession.LocalSteamID, duplicantNetId);
                PacketSender.SendToAllClients(packet);
                
                _lastInventorySync[duplicantNetId] = Time.time;
                DebugConsole.Log($"[ResourceSyncManager] Force synced inventory for duplicant {duplicantNetId}");
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[ResourceSyncManager] Error force syncing inventory {duplicantNetId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Send a resource transfer notification
        /// </summary>
        public void NotifyResourceTransfer(int sourceNetId, int targetNetId, int resourceNetId, 
            string resourceTag, float amount, Vector3 position, ResourceTransferPacket.TransferType transferType)
        {
            if (!MultiplayerSession.IsHost) return;

            try
            {
                var packet = new ResourceTransferPacket(
                    MultiplayerSession.LocalSteamID,
                    sourceNetId,
                    targetNetId,
                    resourceNetId,
                    resourceTag,
                    amount,
                    position,
                    transferType
                );
                
                PacketSender.SendToAllClients(packet);
                DebugConsole.Log($"[ResourceSyncManager] Notified resource transfer: {transferType} - {amount} {resourceTag}");
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[ResourceSyncManager] Error notifying resource transfer: {ex.Message}");
            }
        }

        /// <summary>
        /// Notify about a new storage container that should be monitored
        /// </summary>
        public void RegisterStorage(Storage storage)
        {
            var identity = storage.GetComponent<NetworkIdentity>();
            if (identity != null && !_storageContainers.Contains(storage))
            {
                _storageContainers.Add(storage);
                DebugConsole.Log($"[ResourceSyncManager] Registered new storage container {identity.NetId}");
            }
        }

        /// <summary>
        /// Notify about a new duplicant that should be monitored
        /// </summary>
        public void RegisterDuplicant(MinionIdentity duplicant)
        {
            var identity = duplicant.GetComponent<NetworkIdentity>();
            if (identity != null && !_duplicants.Contains(duplicant))
            {
                _duplicants.Add(duplicant);
                DebugConsole.Log($"[ResourceSyncManager] Registered new duplicant {identity.NetId}");
            }
        }

        /// <summary>
        /// Get synchronization statistics for debugging
        /// </summary>
        public string GetSyncStats()
        {
            return $"Storage: {_storageContainers.Count} containers, {_lastStorageSync.Count} synced\n" +
                   $"Inventory: {_duplicants.Count} duplicants, {_lastInventorySync.Count} synced";
        }
    }
}
