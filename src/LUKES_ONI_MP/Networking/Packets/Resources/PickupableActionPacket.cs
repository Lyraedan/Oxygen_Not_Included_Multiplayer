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
    /// Synchronizes pickupable actions like pickup, drop, store, and retrieve operations.
    /// Prevents double-actions and ensures consistent pickupable state across clients.
    /// </summary>
    public class PickupableActionPacket : IPacket
    {
        public PacketType Type => PacketType.PickupableAction;

        public CSteamID SenderId;
        public int PickupableNetId;      // NetId of the pickupable item
        public int ActorNetId;           // NetId of the duplicant performing action
        public int TargetNetId;          // NetId of target (storage, etc.) or -1 for none
        public PickupAction Action;      // Type of action being performed
        public Vector3 Position;         // Position for the action
        public float Amount;             // Amount being moved (for partial pickups)
        public bool IsCompleted;         // Whether this action is completed

        public enum PickupAction : byte
        {
            Pickup = 0,              // Duplicant picks up item from world
            Drop = 1,                // Duplicant drops item to world
            Store = 2,               // Item stored in container
            Retrieve = 3,            // Item retrieved from container
            Reserve = 4,             // Item reserved for pickup (prevents others)
            Unreserve = 5,           // Item reservation cancelled
            Consume = 6,             // Item consumed/destroyed
            Create = 7               // Item created/spawned
        }

        public PickupableActionPacket() { }

        public PickupableActionPacket(CSteamID senderId, int pickupableNetId, int actorNetId, 
            int targetNetId, PickupAction action, Vector3 position, float amount = 0f, bool isCompleted = true)
        {
            SenderId = senderId;
            PickupableNetId = pickupableNetId;
            ActorNetId = actorNetId;
            TargetNetId = targetNetId;
            Action = action;
            Position = position;
            Amount = amount;
            IsCompleted = isCompleted;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId.m_SteamID);
            writer.Write(PickupableNetId);
            writer.Write(ActorNetId);
            writer.Write(TargetNetId);
            writer.Write((byte)Action);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            writer.Write(Amount);
            writer.Write(IsCompleted);
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = new CSteamID(reader.ReadUInt64());
            PickupableNetId = reader.ReadInt32();
            ActorNetId = reader.ReadInt32();
            TargetNetId = reader.ReadInt32();
            Action = (PickupAction)reader.ReadByte();
            Position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            Amount = reader.ReadSingle();
            IsCompleted = reader.ReadBoolean();
        }

        public void OnDispatched()
        {
            // Only process if this is from another client (prevent local echoing)
            if (SenderId == MultiplayerSession.LocalSteamID)
            {
                return;
            }

            DebugConsole.Log($"[PickupableActionPacket] Processing {Action} for pickupable {PickupableNetId} by actor {ActorNetId}");

            try
            {
                ProcessPickupableAction();
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[PickupableActionPacket] Error processing action: {ex.Message}");
            }
        }

        private void ProcessPickupableAction()
        {
            switch (Action)
            {
                case PickupAction.Pickup:
                    ProcessPickup();
                    break;
                case PickupAction.Drop:
                    ProcessDrop();
                    break;
                case PickupAction.Store:
                    ProcessStore();
                    break;
                case PickupAction.Retrieve:
                    ProcessRetrieve();
                    break;
                case PickupAction.Reserve:
                    ProcessReserve();
                    break;
                case PickupAction.Unreserve:
                    ProcessUnreserve();
                    break;
                case PickupAction.Consume:
                    ProcessConsume();
                    break;
                case PickupAction.Create:
                    ProcessCreate();
                    break;
            }
        }

        private void ProcessPickup()
        {
            if (!NetworkIdentityRegistry.TryGet(PickupableNetId, out NetworkIdentity pickupableIdentity) ||
                !NetworkIdentityRegistry.TryGet(ActorNetId, out NetworkIdentity actorIdentity))
            {
                return;
            }

            var pickupableObj = pickupableIdentity.gameObject;
            var actorObj = actorIdentity.gameObject;
            
            var pickupable = pickupableObj.GetComponent<Pickupable>();
            var actorStorage = actorObj.GetComponent<Storage>();
            
            if (pickupable != null && actorStorage != null)
            {
                // Move pickupable from world to actor inventory
                if (pickupable.storage == null) // Only if not already stored
                {
                    actorStorage.Store(pickupableObj, false, false, true, false);
                    DebugConsole.Log($"[PickupableActionPacket] Actor {ActorNetId} picked up item {PickupableNetId}");
                }
            }
        }

        private void ProcessDrop()
        {
            if (!NetworkIdentityRegistry.TryGet(PickupableNetId, out NetworkIdentity pickupableIdentity) ||
                !NetworkIdentityRegistry.TryGet(ActorNetId, out NetworkIdentity actorIdentity))
            {
                return;
            }

            var pickupableObj = pickupableIdentity.gameObject;
            var actorObj = actorIdentity.gameObject;
            
            var pickupable = pickupableObj.GetComponent<Pickupable>();
            var actorStorage = actorObj.GetComponent<Storage>();
            
            if (pickupable != null && actorStorage != null)
            {
                // Move pickupable from actor inventory to world
                if (pickupable.storage == actorStorage)
                {
                    actorStorage.Drop(pickupableObj, true);
                    pickupableObj.transform.position = Position;
                    DebugConsole.Log($"[PickupableActionPacket] Actor {ActorNetId} dropped item {PickupableNetId} at {Position}");
                }
            }
        }

        private void ProcessStore()
        {
            if (!NetworkIdentityRegistry.TryGet(PickupableNetId, out NetworkIdentity pickupableIdentity) ||
                !NetworkIdentityRegistry.TryGet(TargetNetId, out NetworkIdentity targetIdentity))
            {
                return;
            }

            var pickupableObj = pickupableIdentity.gameObject;
            var targetObj = targetIdentity.gameObject;
            
            var pickupable = pickupableObj.GetComponent<Pickupable>();
            var targetStorage = targetObj.GetComponent<Storage>();
            
            if (pickupable != null && targetStorage != null)
            {
                // Move item to target storage
                if (pickupable.storage != targetStorage && targetStorage.capacityKg > targetStorage.MassStored())
                {
                    // Remove from current storage if any
                    if (pickupable.storage != null)
                    {
                        pickupable.storage.Drop(pickupableObj, false);
                    }
                    
                    targetStorage.Store(pickupableObj, false, false, true, false);
                    DebugConsole.Log($"[PickupableActionPacket] Item {PickupableNetId} stored in container {TargetNetId}");
                }
            }
        }

        private void ProcessRetrieve()
        {
            if (!NetworkIdentityRegistry.TryGet(PickupableNetId, out NetworkIdentity pickupableIdentity) ||
                !NetworkIdentityRegistry.TryGet(TargetNetId, out NetworkIdentity targetIdentity))
            {
                return;
            }

            var pickupableObj = pickupableIdentity.gameObject;
            var targetObj = targetIdentity.gameObject;
            
            if (pickupableObj != null && targetObj != null)
            {
                var pickupable = pickupableObj.GetComponent<Pickupable>();
                var sourceStorage = targetObj.GetComponent<Storage>();
                
                if (pickupable != null && sourceStorage != null)
                {
                    // Remove item from storage
                    if (pickupable.storage == sourceStorage)
                    {
                        sourceStorage.Drop(pickupableObj, true);
                        pickupableObj.transform.position = Position;
                        DebugConsole.Log($"[PickupableActionPacket] Item {PickupableNetId} retrieved from container {TargetNetId}");
                    }
                }
            }
        }

        private void ProcessReserve()
        {
            if (NetworkIdentityRegistry.TryGet(PickupableNetId, out NetworkIdentity pickupableIdentity))
            {
                var pickupableObj = pickupableIdentity.gameObject;
                var pickupable = pickupableObj.GetComponent<Pickupable>();
                if (pickupable != null)
                {
                    // Mark as reserved (add a tag or component to prevent other pickups)
                    pickupableObj.AddTag(GameTags.Stored); // Temporary reservation marker
                    DebugConsole.Log($"[PickupableActionPacket] Item {PickupableNetId} reserved by actor {ActorNetId}");
                }
            }
        }

        private void ProcessUnreserve()
        {
            if (NetworkIdentityRegistry.TryGet(PickupableNetId, out NetworkIdentity pickupableIdentity))
            {
                var pickupableObj = pickupableIdentity.gameObject;
                var pickupable = pickupableObj.GetComponent<Pickupable>();
                if (pickupable != null)
                {
                    // Remove reservation marker
                    pickupableObj.RemoveTag(GameTags.Stored);
                    DebugConsole.Log($"[PickupableActionPacket] Item {PickupableNetId} unreserved");
                }
            }
        }

        private void ProcessConsume()
        {
            if (NetworkIdentityRegistry.TryGet(PickupableNetId, out NetworkIdentity pickupableIdentity))
            {
                var pickupableObj = pickupableIdentity.gameObject;
                var pickupable = pickupableObj.GetComponent<Pickupable>();
                if (pickupable != null)
                {
                    // Remove from any storage first
                    if (pickupable.storage != null)
                    {
                        pickupable.storage.Drop(pickupableObj, false);
                    }
                    
                    // Destroy the object
                    Object.Destroy(pickupableObj);
                    DebugConsole.Log($"[PickupableActionPacket] Item {PickupableNetId} consumed/destroyed");
                }
            }
        }

        private void ProcessCreate()
        {
            // This would be used when spawning new pickupable items
            // The item should already exist locally, so this mainly serves as notification
            DebugConsole.Log($"[PickupableActionPacket] Item {PickupableNetId} created at {Position}");
        }
    }
}
