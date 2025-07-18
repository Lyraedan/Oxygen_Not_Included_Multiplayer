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
    /// Handles resource drops that bypass the WorldDamageSpawnResourcePacket to prevent double resources.
    /// Used for controlled resource spawning that should only happen once across all clients.
    /// </summary>
    public class ResourceDropPacket : IPacket
    {
        public PacketType Type => PacketType.ResourceDrop;

        public CSteamID SenderId;
        public int ResourceNetId;        // NetId to assign to the dropped resource
        public string ElementTag;        // Element type being dropped
        public float Mass;               // Amount of resource
        public float Temperature;        // Temperature of resource
        public byte DiseaseIdx;          // Disease index
        public int DiseaseCount;         // Disease count
        public Vector3 Position;         // World position for drop
        public DropReason Reason;        // Why this resource was dropped
        public int SourceNetId;          // NetId of source (duplicant, storage, etc.)
        public bool PreventLocalSpawn;   // Whether to prevent local spawning

        public enum DropReason : byte
        {
            DuplicantDrop = 0,           // Duplicant manually dropped item
            StorageEjection = 1,         // Storage container ejected item
            DeliveryFailure = 2,         // Failed to deliver to destination
            BuildingDeconstruct = 3,     // Building was deconstructed
            InventoryOverflow = 4,       // Inventory was full
            Death = 5,                   // Duplicant died and dropped items
            ToolUsage = 6,               // Tool usage generated resource
            ManualSpawn = 7              // Manually spawned by host
        }

        public ResourceDropPacket() { }

        public ResourceDropPacket(CSteamID senderId, int resourceNetId, string elementTag, 
            float mass, float temperature, byte diseaseIdx, int diseaseCount, 
            Vector3 position, DropReason reason, int sourceNetId = -1, bool preventLocalSpawn = false)
        {
            SenderId = senderId;
            ResourceNetId = resourceNetId;
            ElementTag = elementTag;
            Mass = mass;
            Temperature = temperature;
            DiseaseIdx = diseaseIdx;
            DiseaseCount = diseaseCount;
            Position = position;
            Reason = reason;
            SourceNetId = sourceNetId;
            PreventLocalSpawn = preventLocalSpawn;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId.m_SteamID);
            writer.Write(ResourceNetId);
            writer.Write(ElementTag);
            writer.Write(Mass);
            writer.Write(Temperature);
            writer.Write(DiseaseIdx);
            writer.Write(DiseaseCount);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            writer.Write((byte)Reason);
            writer.Write(SourceNetId);
            writer.Write(PreventLocalSpawn);
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = new CSteamID(reader.ReadUInt64());
            ResourceNetId = reader.ReadInt32();
            ElementTag = reader.ReadString();
            Mass = reader.ReadSingle();
            Temperature = reader.ReadSingle();
            DiseaseIdx = reader.ReadByte();
            DiseaseCount = reader.ReadInt32();
            Position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            Reason = (DropReason)reader.ReadByte();
            SourceNetId = reader.ReadInt32();
            PreventLocalSpawn = reader.ReadBoolean();
        }

        public void OnDispatched()
        {
            DebugConsole.Log($"[ResourceDropPacket] Processing resource drop: {Mass} {ElementTag} at {Position} (reason: {Reason})");

            // Only process if this is from another client (prevent local echoing)
            if (SenderId == MultiplayerSession.LocalSteamID)
            {
                if (PreventLocalSpawn)
                {
                    DebugConsole.Log("[ResourceDropPacket] Preventing local spawn as requested");
                    return;
                }
                return;
            }

            try
            {
                SpawnDroppedResource();
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[ResourceDropPacket] Error spawning dropped resource: {ex.Message}");
            }
        }

        private void SpawnDroppedResource()
        {
            // Check if resource already exists with this NetId
            var existingObj = NetworkIdentityRegistry.GetGameObject(ResourceNetId);
            if (existingObj != null)
            {
                DebugConsole.Log($"[ResourceDropPacket] Resource {ResourceNetId} already exists, skipping spawn");
                return;
            }

            // Find the element
            var element = ElementLoader.FindElementByName(ElementTag);
            if (element == null)
            {
                DebugConsole.LogError($"[ResourceDropPacket] Could not find element: {ElementTag}");
                return;
            }

            // Validate mass
            if (Mass <= 0f)
            {
                DebugConsole.LogWarning($"[ResourceDropPacket] Invalid mass for {ElementTag}: {Mass}");
                return;
            }

            // Spawn the resource
            var droppedObj = element.substance.SpawnResource(Position, Mass, Temperature, DiseaseIdx, DiseaseCount);
            if (droppedObj != null)
            {
                // Set the network identity
                var identity = droppedObj.GetComponent<NetworkIdentity>();
                if (identity != null)
                {
                    identity.OverrideNetId(ResourceNetId);
                }
                else
                {
                    // Add NetworkIdentity if it doesn't exist
                    identity = droppedObj.AddComponent<NetworkIdentity>();
                    identity.OverrideNetId(ResourceNetId);
                }

                // Add metadata based on drop reason
                switch (Reason)
                {
                    case DropReason.DuplicantDrop:
                        // Mark as recently dropped to prevent immediate re-pickup
                        droppedObj.AddTag(GameTags.Minion);
                        break;
                    case DropReason.StorageEjection:
                        // Mark as ejected from storage
                        break;
                    case DropReason.DeliveryFailure:
                        // Mark as failed delivery
                        break;
                    case DropReason.BuildingDeconstruct:
                        // Mark as building material
                        break;
                    case DropReason.InventoryOverflow:
                        // Mark as overflow
                        break;
                    case DropReason.Death:
                        // Mark as death drop
                        break;
                    case DropReason.ToolUsage:
                        // Mark as tool-generated
                        break;
                    case DropReason.ManualSpawn:
                        // Mark as manually spawned
                        break;
                }

                DebugConsole.Log($"[ResourceDropPacket] Spawned {Mass:F2}kg {ElementTag} at {Position} with NetId {ResourceNetId} (reason: {Reason})");

                // Show popup if significant amount
                if (Mass >= 1f)
                {
                    var pickupable = droppedObj.GetComponent<Pickupable>();
                    if (pickupable != null && pickupable.GetMyWorld() != null)
                    {
                        PopFXManager.Instance.SpawnFX(
                            PopFXManager.Instance.sprite_Resource,
                            Mathf.RoundToInt(Mass) + " " + element.name,
                            droppedObj.transform
                        );
                    }
                }
            }
            else
            {
                DebugConsole.LogError($"[ResourceDropPacket] Failed to spawn resource: {ElementTag}");
            }
        }
    }
}
