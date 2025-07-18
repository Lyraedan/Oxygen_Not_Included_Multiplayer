using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Synchronizes duplicant idle behaviors including downtime activities and recreation.
    /// Coordinates when duplicants are idle, what recreation they choose, and social interactions.
    /// </summary>
    public class IdleBehaviorPacket : IPacket
    {
        public int DuplicantNetId;
        public string IdleActivity;         // "Idle", "Recreation", "Socializing", "Exploring"
        public string RecreationType;       // "WaterCooler", "Arcade", "Telescope", "GreatHall"
        public Vector3 IdlePosition;
        public int RecreationTargetCell;
        public string SocialTarget;         // NetId of other duplicant for socializing
        public float IdleDuration;
        public float RecreationValue;       // How much recreation this provides
        public bool IsGroupActivity;        // Whether this is a group recreation activity
        public float StressLevel;           // Current stress level of the duplicant
        public bool IsScheduledBreak;       // Whether this is a scheduled downtime
        public string IdleReason;           // Reason for being idle

        public PacketType Type => PacketType.IdleBehavior;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(IdleActivity ?? "");
            writer.Write(RecreationType ?? "");
            writer.Write(IdlePosition.x);
            writer.Write(IdlePosition.y);
            writer.Write(IdlePosition.z);
            writer.Write(RecreationTargetCell);
            writer.Write(SocialTarget ?? "");
            writer.Write(IdleDuration);
            writer.Write(RecreationValue);
            writer.Write(IsGroupActivity);
            writer.Write(StressLevel);
            writer.Write(IsScheduledBreak);
            writer.Write(IdleReason ?? "");
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            IdleActivity = reader.ReadString();
            RecreationType = reader.ReadString();
            IdlePosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            RecreationTargetCell = reader.ReadInt32();
            SocialTarget = reader.ReadString();
            IdleDuration = reader.ReadSingle();
            RecreationValue = reader.ReadSingle();
            IsGroupActivity = reader.ReadBoolean();
            StressLevel = reader.ReadSingle();
            IsScheduledBreak = reader.ReadBoolean();
            IdleReason = reader.ReadString();
        }

        public bool IsValid()
        {
            return DuplicantNetId > 0 && !string.IsNullOrEmpty(IdleActivity);
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantNetId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"IdleBehaviorPacket: Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var choreDriver = duplicantGO.GetComponent<ChoreDriver>();
            if (choreDriver == null)
            {
                Debug.LogWarning($"IdleBehaviorPacket: Duplicant {DuplicantNetId} has no ChoreDriver");
                return;
            }

            switch (IdleActivity)
            {
                case "Idle":
                    HandleIdleState(duplicantGO, choreDriver);
                    break;
                case "Recreation":
                    HandleRecreationState(duplicantGO, choreDriver);
                    break;
                case "Socializing":
                    HandleSocializingState(duplicantGO, choreDriver);
                    break;
                case "Exploring":
                    HandleExploringState(duplicantGO, choreDriver);
                    break;
                default:
                    Debug.LogWarning($"Unknown idle activity: {IdleActivity}");
                    break;
            }

            Debug.Log($"Applied idle behavior {IdleActivity} to duplicant {DuplicantNetId}");
        }

        private void HandleIdleState(GameObject duplicantGO, ChoreDriver choreDriver)
        {
            // Simple idle handling
            var consumer = duplicantGO.GetComponent<ChoreConsumer>();
            if (consumer != null)
            {
                try
                {
                    var idleChore = new IdleChore(consumer);
                    if (idleChore != null)
                    {
                        var context = new Chore.Precondition.Context(idleChore, new ChoreConsumerState(consumer), true);
                        idleChore.Begin(context);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to start idle state: {ex.Message}");
                }
            }
        }

        private void HandleRecreationState(GameObject duplicantGO, ChoreDriver choreDriver)
        {
            // Simplified recreation handling
            var consumer = duplicantGO.GetComponent<ChoreConsumer>();
            if (consumer == null) return;

            try
            {
                // Simply create an idle chore with recreation context
                var idleChore = new IdleChore(consumer);
                if (idleChore != null)
                {
                    var context = new Chore.Precondition.Context(idleChore, new ChoreConsumerState(consumer), true);
                    idleChore.Begin(context);
                }
                
                Debug.Log($"Started recreation activity: {RecreationType} for duplicant at {IdlePosition}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to start recreation activity: {ex.Message}");
            }
        }

        private void HandleSocializingState(GameObject duplicantGO, ChoreDriver choreDriver)
        {
            // Simplified socializing handling
            var consumer = duplicantGO.GetComponent<ChoreConsumer>();
            if (consumer != null)
            {
                try
                {
                    var idleChore = new IdleChore(consumer);
                    if (idleChore != null)
                    {
                        var context = new Chore.Precondition.Context(idleChore, new ChoreConsumerState(consumer), true);
                        idleChore.Begin(context);
                    }
                    
                    Debug.Log($"Started socializing activity for duplicant");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to start socializing activity: {ex.Message}");
                }
            }
        }

        private void HandleExploringState(GameObject duplicantGO, ChoreDriver choreDriver)
        {
            // Move duplicant to exploration position
            var navigator = duplicantGO.GetComponent<Navigator>();
            if (navigator != null)
            {
                try
                {
                    int targetCell = Grid.PosToCell(IdlePosition);
                    if (Grid.IsValidCell(targetCell))
                    {
                        navigator.GoTo(targetCell);
                        Debug.Log($"Duplicant {DuplicantNetId} exploring to {IdlePosition}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to start exploration: {ex.Message}");
                }
            }
        }

        public override string ToString()
        {
            return $"IdleBehaviorPacket[DuplicantNetId={DuplicantNetId}, Activity={IdleActivity}, Recreation={RecreationType}, Position={IdlePosition}]";
        }

        public void OnDispatched()
        {
            // Called when packet is sent - cleanup if needed
        }
    }
}
