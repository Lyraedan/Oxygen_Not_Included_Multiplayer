using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Components;
using ONI_MP.Networking;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Resources
{
    /// <summary>
    /// Synchronizes storage container contents across all clients in real-time.
    /// Replaces reliance on hard sync for storage state management.
    /// </summary>
    public class StorageUpdatePacket : IPacket
    {
        public PacketType Type => PacketType.StorageUpdate;

        public CSteamID SenderId;
        public int StorageNetId;                           // NetId of the storage container
        public List<StorageItem> Items = new List<StorageItem>();    // Current contents
        public float TotalMass;                            // Total mass in storage
        public bool IsEmpty;                               // Quick check for empty state

        [System.Serializable]
        public struct StorageItem
        {
            public string ElementTag;      // Element/resource type
            public float Mass;             // Amount stored
            public float Temperature;      // Temperature of stored resource
            public byte DiseaseIdx;        // Disease index
            public int DiseaseCount;       // Disease count
            public bool IsLiquid;          // Is this a liquid element
            public bool IsGas;             // Is this a gas element

            public StorageItem(string elementTag, float mass, float temperature, 
                             byte diseaseIdx, int diseaseCount, bool isLiquid, bool isGas)
            {
                ElementTag = elementTag;
                Mass = mass;
                Temperature = temperature;
                DiseaseIdx = diseaseIdx;
                DiseaseCount = diseaseCount;
                IsLiquid = isLiquid;
                IsGas = isGas;
            }
        }

        public StorageUpdatePacket() { }

        public StorageUpdatePacket(CSteamID senderId, int storageNetId, Storage storage)
        {
            SenderId = senderId;
            StorageNetId = storageNetId;
            
            if (storage == null)
            {
                IsEmpty = true;
                TotalMass = 0f;
                return;
            }

            TotalMass = storage.MassStored();
            IsEmpty = TotalMass <= 0f;

            // Extract all storage contents
            Items.Clear();
            
            var items = storage.items;
            for (int i = 0; i < items.Count; i++)
            {
                var gameObject = items[i];
                if (gameObject != null)
                {
                    var primaryElement = gameObject.GetComponent<PrimaryElement>();
                    if (primaryElement != null)
                    {
                        var element = primaryElement.Element;
                        if (element != null)
                        {
                            var storageItem = new StorageItem(
                                element.tag.Name,
                                primaryElement.Mass,
                                primaryElement.Temperature,
                                primaryElement.DiseaseIdx,
                                primaryElement.DiseaseCount,
                                element.IsLiquid,
                                element.IsGas
                            );
                            Items.Add(storageItem);
                        }
                    }
                }
            }

            DebugConsole.Log($"[StorageUpdatePacket] Created packet for storage {StorageNetId}: {Items.Count} items, {TotalMass:F2}kg total");
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId.m_SteamID);
            writer.Write(StorageNetId);
            writer.Write(TotalMass);
            writer.Write(IsEmpty);
            
            writer.Write(Items.Count);
            foreach (var item in Items)
            {
                writer.Write(item.ElementTag);
                writer.Write(item.Mass);
                writer.Write(item.Temperature);
                writer.Write(item.DiseaseIdx);
                writer.Write(item.DiseaseCount);
                writer.Write(item.IsLiquid);
                writer.Write(item.IsGas);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = new CSteamID(reader.ReadUInt64());
            StorageNetId = reader.ReadInt32();
            TotalMass = reader.ReadSingle();
            IsEmpty = reader.ReadBoolean();
            
            int itemCount = reader.ReadInt32();
            Items = new List<StorageItem>();
            
            for (int i = 0; i < itemCount; i++)
            {
                var item = new StorageItem(
                    reader.ReadString(),    // ElementTag
                    reader.ReadSingle(),    // Mass
                    reader.ReadSingle(),    // Temperature
                    reader.ReadByte(),      // DiseaseIdx
                    reader.ReadInt32(),     // DiseaseCount
                    reader.ReadBoolean(),   // IsLiquid
                    reader.ReadBoolean()    // IsGas
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

            DebugConsole.Log($"[StorageUpdatePacket] Processing storage update for {StorageNetId}: {Items.Count} items");

            try
            {
                UpdateStorageContents();
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[StorageUpdatePacket] Error updating storage: {ex.Message}");
            }
        }

        private void UpdateStorageContents()
        {
            if (!NetworkIdentityRegistry.TryGet(StorageNetId, out NetworkIdentity storageIdentity))
            {
                DebugConsole.LogWarning($"[StorageUpdatePacket] Storage with NetId {StorageNetId} not found");
                return;
            }

            var storageObj = storageIdentity.gameObject;
            var storage = storageObj.GetComponent<Storage>();
            if (storage == null)
            {
                DebugConsole.LogWarning($"[StorageUpdatePacket] No Storage component on object {StorageNetId}");
                return;
            }

            // Temporarily disable storage notifications to prevent feedback loops
            bool wasNotifying = storage.ShouldShowInUI();
            
            try
            {
                // Clear existing contents
                storage.DropAll(false, false, default(Vector3), true);
                
                if (IsEmpty)
                {
                    DebugConsole.Log($"[StorageUpdatePacket] Storage {StorageNetId} is now empty");
                    return;
                }

                // Add all items from the packet
                foreach (var item in Items)
                {
                    var element = ElementLoader.FindElementByName(item.ElementTag);
                    if (element != null && item.Mass > 0f)
                    {
                        // Add the resource to storage
                        storage.AddOre(element.id, item.Mass, item.Temperature, 
                                     item.DiseaseIdx, item.DiseaseCount);
                                     
                        DebugConsole.Log($"[StorageUpdatePacket] Added {item.Mass:F2}kg {item.ElementTag} to storage {StorageNetId}");
                    }
                }

                DebugConsole.Log($"[StorageUpdatePacket] Updated storage {StorageNetId} with {Items.Count} items, total mass: {TotalMass:F2}kg");
            }
            finally
            {
                // Restore notification state
                // Note: ShouldShowInUI() is read-only, this is for reference
            }
        }
    }
}
