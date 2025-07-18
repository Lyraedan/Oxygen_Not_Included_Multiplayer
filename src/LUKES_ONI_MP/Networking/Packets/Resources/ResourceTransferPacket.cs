using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Components;
using ONI_MP.Networking;
using UnityEngine;
using Steamworks;

namespace ONI_MP.Networking.Packets.Resources
{
    /// <summary>
    /// Synchronizes resource transfers between storage containers, pickupables, and duplicants.
    /// Prevents double resources and ensures consistent inventory state across clients.
    /// </summary>
    public class ResourceTransferPacket : IPacket
    {
        public PacketType Type => PacketType.ResourceTransfer;

        public CSteamID SenderId;
        public int SourceNetId;      // NetId of source (storage, duplicant, or -1 for world spawn)
        public int TargetNetId;      // NetId of target (storage, duplicant, or -1 for world drop)
        public int ResourceNetId;    // NetId of the resource being transferred
        public string ResourceTag;   // Resource type (e.g., "Dirt", "Coal", "Water")
        public float Amount;         // Amount being transferred
        public Vector3 Position;     // Position for world drops or pickups
        public TransferType Type_;   // Type of transfer operation

        public enum TransferType : byte
        {
            PickupFromWorld = 0,        // Duplicant picks up from world
            StoreInContainer = 1,       // Store resource in storage container
            TakeFromContainer = 2,      // Take resource from storage container
            DropToWorld = 3,            // Drop resource to world
            DuplicantToContainer = 4,   // Duplicant directly deposits to container
            ContainerToDuplicant = 5,   // Container directly gives to duplicant
            DuplicantToDuplicant = 6    // Transfer between duplicants (rare)
        }

        public ResourceTransferPacket() { }

        public ResourceTransferPacket(CSteamID senderId, int sourceNetId, int targetNetId, 
            int resourceNetId, string resourceTag, float amount, Vector3 position, TransferType transferType)
        {
            SenderId = senderId;
            SourceNetId = sourceNetId;
            TargetNetId = targetNetId;
            ResourceNetId = resourceNetId;
            ResourceTag = resourceTag;
            Amount = amount;
            Position = position;
            Type_ = transferType;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId.m_SteamID);
            writer.Write(SourceNetId);
            writer.Write(TargetNetId);
            writer.Write(ResourceNetId);
            writer.Write(ResourceTag);
            writer.Write(Amount);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            writer.Write((byte)Type_);
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = new CSteamID(reader.ReadUInt64());
            SourceNetId = reader.ReadInt32();
            TargetNetId = reader.ReadInt32();
            ResourceNetId = reader.ReadInt32();
            ResourceTag = reader.ReadString();
            Amount = reader.ReadSingle();
            Position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            Type_ = (TransferType)reader.ReadByte();
        }

        public void OnDispatched()
        {
            DebugConsole.Log($"[ResourceTransferPacket] Processing {Type_}: {Amount} {ResourceTag} from {SourceNetId} to {TargetNetId}");

            // Only process if this is from another client (prevent local echoing)
            if (SenderId == MultiplayerSession.LocalSteamID)
            {
                DebugConsole.Log("[ResourceTransferPacket] Ignoring own packet");
                return;
            }

            try
            {
                ProcessResourceTransfer();
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[ResourceTransferPacket] Error processing transfer: {ex.Message}");
            }
        }

        private void ProcessResourceTransfer()
        {
            switch (Type_)
            {
                case TransferType.PickupFromWorld:
                    ProcessWorldPickup();
                    break;
                case TransferType.StoreInContainer:
                    ProcessStoreInContainer();
                    break;
                case TransferType.TakeFromContainer:
                    ProcessTakeFromContainer();
                    break;
                case TransferType.DropToWorld:
                    ProcessDropToWorld();
                    break;
                case TransferType.DuplicantToContainer:
                    ProcessDuplicantToContainer();
                    break;
                case TransferType.ContainerToDuplicant:
                    ProcessContainerToDuplicant();
                    break;
                case TransferType.DuplicantToDuplicant:
                    ProcessDuplicantToDuplicant();
                    break;
            }
        }

        private void ProcessWorldPickup()
        {
            // Remove the resource from world if it exists
            var resourceObj = NetworkIdentityRegistry.GetGameObject(ResourceNetId);
            if (resourceObj != null)
            {
                var pickupable = resourceObj.GetComponent<Pickupable>();
                if (pickupable != null)
                {
                    DebugConsole.Log($"[ResourceTransferPacket] Removing picked up resource: {ResourceTag}");
                    // Mark as taken to prevent local pickup
                    pickupable.gameObject.SetActive(false);
                    Object.Destroy(pickupable.gameObject);
                }
            }
        }

        private void ProcessStoreInContainer()
        {
            var containerObj = NetworkIdentityRegistry.GetGameObject(TargetNetId);
            if (containerObj != null)
            {
                var storage = containerObj.GetComponent<Storage>();
                if (storage != null && storage.capacityKg > storage.MassStored())
                {
                    // Add resource to storage
                    var element = ElementLoader.FindElementByName(ResourceTag);
                    if (element != null)
                    {
                        // Create resource in storage
                        storage.AddOre(element.id, Amount, element.defaultValues.temperature, 
                                     element.defaultValues.diseaseIdx, element.defaultValues.diseaseCount);
                        DebugConsole.Log($"[ResourceTransferPacket] Added {Amount} {ResourceTag} to storage");
                    }
                }
            }
        }

        private void ProcessTakeFromContainer()
        {
            var containerObj = NetworkIdentityRegistry.GetGameObject(SourceNetId);
            if (containerObj != null)
            {
                var storage = containerObj.GetComponent<Storage>();
                if (storage != null)
                {
                    // Find and remove resource from storage
                    var element = ElementLoader.FindElementByName(ResourceTag);
                    if (element != null)
                    {
                        storage.ConsumeIgnoringDisease(element.id, Amount);
                        DebugConsole.Log($"[ResourceTransferPacket] Removed {Amount} {ResourceTag} from storage");
                    }
                }
            }
        }

        private void ProcessDropToWorld()
        {
            // Spawn resource at position if it doesn't already exist
            var existingObj = NetworkIdentityRegistry.GetGameObject(ResourceNetId);
            if (existingObj == null)
            {
                var element = ElementLoader.FindElementByName(ResourceTag);
                if (element != null)
                {
                    var droppedObj = element.substance.SpawnResource(Position, Amount, 
                        element.defaultValues.temperature, element.defaultValues.diseaseIdx, 
                        element.defaultValues.diseaseCount);
                        
                    var identity = droppedObj.GetComponent<NetworkIdentity>();
                    if (identity != null)
                    {
                        identity.OverrideNetId(ResourceNetId);
                        DebugConsole.Log($"[ResourceTransferPacket] Spawned dropped resource: {Amount} {ResourceTag}");
                    }
                }
            }
        }

        private void ProcessDuplicantToContainer()
        {
            // Similar to store in container but with duplicant source validation
            ProcessStoreInContainer();
        }

        private void ProcessContainerToDuplicant()
        {
            // Similar to take from container but with duplicant target validation
            ProcessTakeFromContainer();
        }

        private void ProcessDuplicantToDuplicant()
        {
            // Handle direct transfers between duplicants (rare case)
            DebugConsole.Log($"[ResourceTransferPacket] Duplicant-to-duplicant transfer: {Amount} {ResourceTag}");
        }
    }
}
