using System;
using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Synchronizes work assignments and chore priorities across multiplayer clients.
    /// Coordinates which duplicants are assigned to which chores and their execution order.
    /// </summary>
    public class WorkAssignmentPacket : IPacket
    {
        public int DuplicantNetId;
        public string ChoreTypeId;
        public int TargetCell;
        public Vector3 TargetPosition;
        public string TargetPrefabId;
        public float Priority;
        public string WorkerRole;              // Mining, Construction, Cooking, etc.
        public float TimeEstimate;             // Estimated time to complete
        public List<string> RequiredSkills;    // Skills needed for this work
        public bool IsUrgent;                  // Emergency or high-priority work
        public string AssignmentReason;        // Why this duplicant was chosen
        public System.DateTime AssignedAt;

        public PacketType Type => PacketType.WorkAssignment;

        public WorkAssignmentPacket()
        {
            RequiredSkills = new List<string>();
            AssignedAt = System.DateTime.UtcNow;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(ChoreTypeId ?? "");
            writer.Write(TargetCell);
            
            writer.Write(TargetPosition.x);
            writer.Write(TargetPosition.y);
            writer.Write(TargetPosition.z);
            
            writer.Write(TargetPrefabId ?? "");
            writer.Write(Priority);
            writer.Write(WorkerRole ?? "");
            writer.Write(TimeEstimate);
            
            writer.Write(RequiredSkills.Count);
            foreach (var skill in RequiredSkills)
            {
                writer.Write(skill ?? "");
            }
            
            writer.Write(IsUrgent);
            writer.Write(AssignmentReason ?? "");
            writer.Write(AssignedAt.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            ChoreTypeId = reader.ReadString();
            TargetCell = reader.ReadInt32();
            
            TargetPosition = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            TargetPrefabId = reader.ReadString();
            Priority = reader.ReadSingle();
            WorkerRole = reader.ReadString();
            TimeEstimate = reader.ReadSingle();
            
            RequiredSkills.Clear();
            int skillCount = reader.ReadInt32();
            for (int i = 0; i < skillCount; i++)
            {
                RequiredSkills.Add(reader.ReadString());
            }
            
            IsUrgent = reader.ReadBoolean();
            AssignmentReason = reader.ReadString();
            AssignedAt = System.DateTime.FromBinary(reader.ReadInt64());
        }

        public void OnDispatched()
        {
            if (!NetworkIdentityRegistry.TryGet(DuplicantNetId, out var entity))
            {
                DebugConsole.LogWarning($"[WorkAssignmentPacket] Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var duplicantGO = entity.gameObject;
            var choreConsumer = duplicantGO.GetComponent<ChoreConsumer>();
            var choreDriver = duplicantGO.GetComponent<ChoreDriver>();

            if (choreConsumer == null || choreDriver == null)
            {
                DebugConsole.LogWarning($"[WorkAssignmentPacket] Duplicant {DuplicantNetId} missing required chore components");
                return;
            }

            // Cancel current chore if it's not the same type
            var currentChore = choreDriver.GetCurrentChore();
            if (currentChore != null && currentChore.choreType.Id != ChoreTypeId)
            {
                currentChore.Cancel("Overridden by work assignment packet");
                DebugConsole.Log($"[WorkAssignmentPacket] Cancelled current chore {currentChore.choreType.Id} for {ChoreTypeId}");
            }

            // Try to create and assign the new chore
            try
            {
                var choreType = Db.Get().ChoreTypes.Get(ChoreTypeId);
                if (choreType == null)
                {
                    DebugConsole.LogWarning($"[WorkAssignmentPacket] Unknown chore type: {ChoreTypeId}");
                    return;
                }

                // Build precondition context
                var context = new Chore.Precondition.Context
                {
                    consumerState = new ChoreConsumerState(choreConsumer),
                    choreTypeForPermission = choreType
                };

                // Use ChoreFactory to create the chore
                var newChore = ONI_MP.Misc.ChoreFactory.Create(
                    ChoreTypeId, 
                    context, 
                    duplicantGO, 
                    TargetPosition, 
                    TargetCell, 
                    TargetPrefabId
                );

                if (newChore != null)
                {
                    // Assign priority if applicable
                    if (newChore.gameObject != null && newChore.gameObject.TryGetComponent<Prioritizable>(out var prioritizable))
                    {
                        var priorityValue = Mathf.RoundToInt(Priority);
                        var priorityClass = IsUrgent ? PriorityScreen.PriorityClass.high : PriorityScreen.PriorityClass.basic;
                        prioritizable.SetMasterPriority(new PrioritySetting(priorityClass, priorityValue));
                    }

                    newChore.Begin(context);
                    DebugConsole.Log($"[WorkAssignmentPacket] Assigned {ChoreTypeId} to duplicant {DuplicantNetId} (Role: {WorkerRole}, Priority: {Priority})");
                }
                else
                {
                    DebugConsole.LogWarning($"[WorkAssignmentPacket] Failed to create chore {ChoreTypeId}");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogException(ex);
            }
        }
    }
}
