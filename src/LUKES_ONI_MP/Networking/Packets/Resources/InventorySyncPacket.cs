using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Components;
using ONI_MP.Networking;
using Steamworks;

namespace ONI_MP.Networking.Packets.Resources
{
    /// <summary>
    /// Synchronizes duplicant inventory contents across all clients.
    /// Tracks what each duplicant is carrying, including partial resources.
    /// </summary>
    public class InventorySyncPacket : IPacket
    {
        public PacketType Type => PacketType.InventorySync;

        public CSteamID SenderId;
        public int DuplicantNetId;                         // NetId of the duplicant
        public List<InventoryItem> Items = new List<InventoryItem>(); // What they're carrying
        public float TotalCarriedMass;                     // Total mass being carried
        public bool IsCarryingAnything;                    // Quick check for empty inventory

        [System.Serializable]
        public struct InventoryItem
        {
            public string ElementTag;      // Element/resource type
            public float Mass;             // Amount carried
            public float Temperature;      // Temperature of carried resource
            public byte DiseaseIdx;        // Disease index
            public int DiseaseCount;       // Disease count
            public int PickupableNetId;    // NetId of the pickupable object (if applicable)
            public InventorySlot Slot;     // Where in inventory this item is

            public InventoryItem(string elementTag, float mass, float temperature, 
                               byte diseaseIdx, int diseaseCount, int pickupableNetId, InventorySlot slot)
            {
                ElementTag = elementTag;
                Mass = mass;
                Temperature = temperature;
                DiseaseIdx = diseaseIdx;
                DiseaseCount = diseaseCount;
                PickupableNetId = pickupableNetId;
                Slot = slot;
            }
        }

        public enum InventorySlot : byte
        {
            Hands = 0,          // What they're actively carrying
            Storage = 1,        // In their inventory storage
            Equipment = 2,      // Equipment slot
            Outfit = 3,         // Clothing/outfit
            Food = 4            // Food storage
        }

        public InventorySyncPacket() { }

        public InventorySyncPacket(CSteamID senderId, int duplicantNetId)
        {
            SenderId = senderId;
            DuplicantNetId = duplicantNetId;
            
            if (!NetworkIdentityRegistry.TryGet(duplicantNetId, out NetworkIdentity duplicantIdentity))
            {
                IsCarryingAnything = false;
                TotalCarriedMass = 0f;
                return;
            }

            var duplicantObj = duplicantIdentity.gameObject;
            // Extract inventory from duplicant
            ExtractDuplicantInventory(duplicantObj);
        }

        private void ExtractDuplicantInventory(UnityEngine.GameObject duplicantObj)
        {
            Items.Clear();
            TotalCarriedMass = 0f;

            // Check for Storage component (duplicant inventory)
            var storage = duplicantObj.GetComponent<Storage>();
            if (storage != null)
            {
                var items = storage.items;
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item != null)
                    {
                        var primaryElement = item.GetComponent<PrimaryElement>();
                        var networkIdentity = item.GetComponent<NetworkIdentity>();
                        
                        if (primaryElement != null)
                        {
                            var inventoryItem = new InventoryItem(
                                primaryElement.Element.tag.Name,
                                primaryElement.Mass,
                                primaryElement.Temperature,
                                primaryElement.DiseaseIdx,
                                primaryElement.DiseaseCount,
                                networkIdentity?.NetId ?? -1,
                                InventorySlot.Storage
                            );
                            Items.Add(inventoryItem);
                            TotalCarriedMass += primaryElement.Mass;
                        }
                    }
                }
            }

            // Check for what they're currently holding (ManualDeliveryKG, etc.)
            var manualDelivery = duplicantObj.GetComponent<ManualDeliveryKG>();
            if (manualDelivery != null)
            {
                // Check if they're carrying something for delivery
                var carryComponent = duplicantObj.GetComponent<MinionIdentity>();
                if (carryComponent != null)
                {
                    // Check for carried pickupables
                    var pickupables = duplicantObj.GetComponentsInChildren<Pickupable>();
                    foreach (var pickupable in pickupables)
                    {
                        if (pickupable.storage == storage) continue; // Already counted in storage

                        var primaryElement = pickupable.GetComponent<PrimaryElement>();
                        var networkIdentity = pickupable.GetComponent<NetworkIdentity>();
                        
                        if (primaryElement != null)
                        {
                            var inventoryItem = new InventoryItem(
                                primaryElement.Element.tag.Name,
                                primaryElement.Mass,
                                primaryElement.Temperature,
                                primaryElement.DiseaseIdx,
                                primaryElement.DiseaseCount,
                                networkIdentity?.NetId ?? -1,
                                InventorySlot.Hands
                            );
                            Items.Add(inventoryItem);
                            TotalCarriedMass += primaryElement.Mass;
                        }
                    }
                }
            }

            IsCarryingAnything = TotalCarriedMass > 0f;
            DebugConsole.Log($"[InventorySyncPacket] Duplicant {DuplicantNetId} carrying {Items.Count} items, {TotalCarriedMass:F2}kg total");
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId.m_SteamID);
            writer.Write(DuplicantNetId);
            writer.Write(TotalCarriedMass);
            writer.Write(IsCarryingAnything);
            
            writer.Write(Items.Count);
            foreach (var item in Items)
            {
                writer.Write(item.ElementTag);
                writer.Write(item.Mass);
                writer.Write(item.Temperature);
                writer.Write(item.DiseaseIdx);
                writer.Write(item.DiseaseCount);
                writer.Write(item.PickupableNetId);
                writer.Write((byte)item.Slot);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = new CSteamID(reader.ReadUInt64());
            DuplicantNetId = reader.ReadInt32();
            TotalCarriedMass = reader.ReadSingle();
            IsCarryingAnything = reader.ReadBoolean();
            
            int itemCount = reader.ReadInt32();
            Items = new List<InventoryItem>();
            
            for (int i = 0; i < itemCount; i++)
            {
                var item = new InventoryItem(
                    reader.ReadString(),            // ElementTag
                    reader.ReadSingle(),            // Mass
                    reader.ReadSingle(),            // Temperature
                    reader.ReadByte(),              // DiseaseIdx
                    reader.ReadInt32(),             // DiseaseCount
                    reader.ReadInt32(),             // PickupableNetId
                    (InventorySlot)reader.ReadByte() // Slot
                );
                Items.Add(item);
            }
        }

        public void OnDispatched()
        {
            // Only process if this is from another client (prevent local echoing)
            if (SenderId == MultiplayerSession.LocalSteamID)
            {
                return;
            }

            DebugConsole.Log($"[InventorySyncPacket] Processing inventory sync for duplicant {DuplicantNetId}: {Items.Count} items");

            try
            {
                UpdateDuplicantInventory();
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[InventorySyncPacket] Error updating duplicant inventory: {ex.Message}");
            }
        }

        private void UpdateDuplicantInventory()
        {
            if (!NetworkIdentityRegistry.TryGet(DuplicantNetId, out NetworkIdentity duplicantIdentity))
            {
                DebugConsole.LogWarning($"[InventorySyncPacket] Duplicant with NetId {DuplicantNetId} not found");
                return;
            }

            var duplicantObj = duplicantIdentity.gameObject;
            var storage = duplicantObj.GetComponent<Storage>();
            if (storage == null)
            {
                DebugConsole.LogWarning($"[InventorySyncPacket] No Storage component on duplicant {DuplicantNetId}");
                return;
            }

            try
            {
                // Clear existing inventory (carefully to avoid breaking duplicant state)
                var existingItems = new List<UnityEngine.GameObject>(storage.items);
                foreach (var item in existingItems)
                {
                    if (item != null)
                    {
                        // Only remove if it's a resource, not equipment
                        var pickupable = item.GetComponent<Pickupable>();
                        if (pickupable != null && item.HasTag(GameTags.Pickupable))
                        {
                            storage.Drop(item, true);
                        }
                    }
                }

                if (!IsCarryingAnything)
                {
                    DebugConsole.Log($"[InventorySyncPacket] Duplicant {DuplicantNetId} inventory is now empty");
                    return;
                }

                // Add all items from the packet
                foreach (var item in Items)
                {
                    if (item.Slot == InventorySlot.Storage)
                    {
                        var element = ElementLoader.FindElementByName(item.ElementTag);
                        if (element != null && item.Mass > 0f)
                        {
                            // Create the resource and add to duplicant inventory
                            var resourceObj = element.substance.SpawnResource(
                                duplicantObj.transform.position, item.Mass, item.Temperature, 
                                item.DiseaseIdx, item.DiseaseCount);
                                
                            if (resourceObj != null)
                            {
                                var pickupable = resourceObj.GetComponent<Pickupable>();
                                if (pickupable != null)
                                {
                                    // Override NetId if specified
                                    if (item.PickupableNetId != -1)
                                    {
                                        var identity = resourceObj.GetComponent<NetworkIdentity>();
                                        if (identity != null)
                                        {
                                            identity.OverrideNetId(item.PickupableNetId);
                                        }
                                    }
                                    
                                    // Add to duplicant storage
                                    storage.Store(resourceObj, false, false, true, false);
                                    DebugConsole.Log($"[InventorySyncPacket] Added {item.Mass:F2}kg {item.ElementTag} to duplicant {DuplicantNetId} inventory");
                                }
                            }
                        }
                    }
                }

                DebugConsole.Log($"[InventorySyncPacket] Updated duplicant {DuplicantNetId} inventory with {Items.Count} items, total mass: {TotalCarriedMass:F2}kg");
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[InventorySyncPacket] Error in UpdateDuplicantInventory: {ex.Message}");
            }
        }
    }
}
