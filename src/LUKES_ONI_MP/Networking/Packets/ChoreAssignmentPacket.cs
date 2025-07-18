using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.Chores;
using System;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets
{
    /// <summary>
    /// Immediate chore assignment packet for direct duplicant work coordination.
    /// Provides fast, lightweight chore assignments without the complexity of WorkAssignmentPacket.
    /// Used for urgent or simple chore assignments that need immediate execution.
    /// </summary>
    public class ChoreAssignmentPacket : IPacket
    {
        public int NetId;
        public string ChoreTypeId;
        public Vector3 TargetPosition;
        public int TargetCell = -1;
        public string TargetPrefabId; // optional
        public float Priority = 5.0f;
        public bool IsImmediate; // Whether this assignment should override current chore
        public bool IsUrgent;    // Whether this is an urgent assignment
        public System.DateTime AssignedAt;

        public PacketType Type => PacketType.ChoreAssignment;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetId);
            writer.Write(ChoreTypeId ?? string.Empty);
            writer.Write(TargetPosition.x);
            writer.Write(TargetPosition.y);
            writer.Write(TargetPosition.z);
            writer.Write(TargetCell);
            writer.Write(TargetPrefabId ?? string.Empty);
            writer.Write(Priority);
            writer.Write(IsImmediate);
            writer.Write(IsUrgent);
            writer.Write(System.DateTime.UtcNow.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            NetId = reader.ReadInt32();
            ChoreTypeId = reader.ReadString();
            TargetPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            TargetCell = reader.ReadInt32();
            TargetPrefabId = reader.ReadString();
            Priority = reader.ReadSingle();
            IsImmediate = reader.ReadBoolean();
            IsUrgent = reader.ReadBoolean();
            AssignedAt = System.DateTime.FromBinary(reader.ReadInt64());
        }

        public bool IsValid()
        {
            return NetId > 0 && !string.IsNullOrEmpty(ChoreTypeId);
        }

        public void Execute()
        {
            if (!NetworkIdentityRegistry.TryGet(NetId, out var entity))
            {
                DebugConsole.LogWarning($"[ChoreAssignment] Could not find entity with NetId {NetId}");
                return;
            }

            var dupeGO = entity.gameObject;
            var consumer = dupeGO.GetComponent<ChoreConsumer>();
            var choreDriver = dupeGO.GetComponent<ChoreDriver>();

            if (consumer == null || choreDriver == null)
            {
                DebugConsole.LogWarning($"[ChoreAssignment] Missing required chore components on duplicant {NetId}");
                return;
            }

            var choreType = Db.Get().ChoreTypes.Get(ChoreTypeId);
            if (choreType == null)
            {
                DebugConsole.LogWarning($"[ChoreAssignment] Unknown chore type: {ChoreTypeId}");
                return;
            }

            try
            {
                // Handle immediate assignments (cancel current chore)
                if (IsImmediate)
                {
                    var currentChore = choreDriver.GetCurrentChore();
                    if (currentChore != null)
                    {
                        currentChore.Cancel("Overridden by immediate chore assignment");
                        DebugConsole.Log($"[ChoreAssignment] Cancelled current chore for immediate assignment: {ChoreTypeId}");
                    }
                }

                // Create the chore using ChoreFactory
                var context = CreatePreconditionContext(consumer, choreType);
                var newChore = Misc.ChoreFactory.Create(
                    ChoreTypeId, 
                    context, 
                    dupeGO, 
                    TargetPosition, 
                    TargetCell, 
                    TargetPrefabId
                );

                if (newChore != null)
                {
                    // Apply priority settings
                    ApplyChoreSettings(newChore, dupeGO);
                    
                    // Assign the chore to the duplicant
                    newChore.AssignChoreToDuplicant(dupeGO);
                    
                    DebugConsole.Log($"[ChoreAssignment] Successfully assigned {ChoreTypeId} to duplicant {NetId} " +
                                   $"(Priority: {Priority}, Urgent: {IsUrgent}, Immediate: {IsImmediate})");
                }
                else
                {
                    DebugConsole.LogWarning($"[ChoreAssignment] Failed to create chore: {ChoreTypeId} for duplicant {NetId}");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogException(ex);
            }
        }

        private Chore.Precondition.Context CreatePreconditionContext(ChoreConsumer consumer, ChoreType choreType)
        {
            var choreConsumerState = consumer.GetComponent<ChoreConsumerState>();
            if (choreConsumerState == null)
            {
                // Create a new ChoreConsumerState if it doesn't exist
                choreConsumerState = new ChoreConsumerState(consumer);
            }

            return new Chore.Precondition.Context
            {
                consumerState = choreConsumerState,
                choreTypeForPermission = choreType
            };
        }

        private void ApplyChoreSettings(Chore chore, GameObject dupeGO)
        {
            try
            {
                // Apply priority if the chore supports it
                if (chore.gameObject != null && chore.gameObject.TryGetComponent<Prioritizable>(out var prioritizable))
                {
                    var priorityValue = Mathf.RoundToInt(Priority);
                    var priorityClass = IsUrgent ? PriorityScreen.PriorityClass.high : PriorityScreen.PriorityClass.basic;
                    var prioritySetting = new PrioritySetting(priorityClass, priorityValue);
                    
                    prioritizable.SetMasterPriority(prioritySetting);
                    DebugConsole.Log($"[ChoreAssignment] Applied priority {priorityValue} ({priorityClass}) to chore {ChoreTypeId}");
                }

                // Set urgency status if applicable
                if (IsUrgent && chore is IUrgentChore urgentChore)
                {
                    urgentChore.SetUrgent(true);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ChoreAssignment] Failed to apply chore settings: {ex.Message}");
            }
        }

        public void OnDispatched()
        {
            // Called after packet is sent - cleanup if needed
        }

        public override string ToString()
        {
            return $"ChoreAssignmentPacket[NetId={NetId}, ChoreType={ChoreTypeId}, " +
                   $"Priority={Priority}, Urgent={IsUrgent}, Immediate={IsImmediate}]";
        }
    }

    /// <summary>
    /// Interface for chores that support urgency settings
    /// </summary>
    public interface IUrgentChore
    {
        void SetUrgent(bool urgent);
    }
}
