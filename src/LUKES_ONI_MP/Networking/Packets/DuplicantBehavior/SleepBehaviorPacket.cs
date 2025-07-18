using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Synchronizes duplicant sleep behaviors including bed assignments and rest patterns.
    /// Coordinates sleep cycles, bed usage, and sleep quality across multiplayer clients.
    /// </summary>
    public class SleepBehaviorPacket : IPacket
    {
        public int DuplicantNetId;
        public string SleepState;           // "GoingToBed", "Sleeping", "WakingUp", "Tired", "Rested"
        public int BedNetId;                // NetworkId of assigned bed
        public Vector3 BedPosition;         // Position of the bed
        public float SleepDuration;         // How long they've been sleeping
        public float TirednessLevel;        // Current tiredness (0-100)
        public float SleepQuality;          // Quality of sleep (affected by room, noise, etc.)
        public bool IsScheduledSleep;       // Is this scheduled sleep or emergency sleep?
        public string SleepDisruption;      // Reason for sleep disruption ("Noise", "Light", "Stress", "None")
        public float SleepStartTime;        // Game time when sleep started
        public float ExpectedWakeTime;      // Game time when they should wake up
        public DateTime PacketTime;

        public PacketType Type => PacketType.SleepBehavior;

        public SleepBehaviorPacket()
        {
            PacketTime = DateTime.UtcNow;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(SleepState ?? "");
            writer.Write(BedNetId);
            
            writer.Write(BedPosition.x);
            writer.Write(BedPosition.y);
            writer.Write(BedPosition.z);
            
            writer.Write(SleepDuration);
            writer.Write(TirednessLevel);
            writer.Write(SleepQuality);
            writer.Write(IsScheduledSleep);
            writer.Write(SleepDisruption ?? "");
            writer.Write(SleepStartTime);
            writer.Write(ExpectedWakeTime);
            writer.Write(PacketTime.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            SleepState = reader.ReadString();
            BedNetId = reader.ReadInt32();
            
            BedPosition = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            SleepDuration = reader.ReadSingle();
            TirednessLevel = reader.ReadSingle();
            SleepQuality = reader.ReadSingle();
            IsScheduledSleep = reader.ReadBoolean();
            SleepDisruption = reader.ReadString();
            SleepStartTime = reader.ReadSingle();
            ExpectedWakeTime = reader.ReadSingle();
            PacketTime = DateTime.FromBinary(reader.ReadInt64());
        }

        public void OnDispatched()
        {
            if (!NetworkIdentityRegistry.TryGet(DuplicantNetId, out var entity))
            {
                DebugConsole.LogWarning($"[SleepBehaviorPacket] Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var duplicantGO = entity.gameObject;
            var choreDriver = duplicantGO.GetComponent<ChoreDriver>();
            var navigator = duplicantGO.GetComponent<Navigator>();
                var stamina = duplicantGO.GetComponent<Klei.AI.AttributeInstance>();            if (choreDriver == null || navigator == null)
            {
                DebugConsole.LogWarning($"[SleepBehaviorPacket] Duplicant {DuplicantNetId} missing required components");
                return;
            }

            try
            {
                // Handle different sleep states
                switch (SleepState)
                {
                    case "GoingToBed":
                        HandleGoingToBedState(duplicantGO, choreDriver, navigator);
                        break;
                        
                    case "Sleeping":
                        HandleSleepingState(duplicantGO, choreDriver, stamina);
                        break;
                        
                    case "WakingUp":
                        HandleWakingUpState(duplicantGO, choreDriver);
                        break;
                        
                    case "Tired":
                        HandleTiredState(duplicantGO, choreDriver, stamina);
                        break;
                        
                    case "Rested":
                        HandleRestedState(duplicantGO, choreDriver, stamina);
                        break;
                        
                    default:
                        DebugConsole.LogWarning($"[SleepBehaviorPacket] Unknown sleep state: {SleepState}");
                        break;
                }

                // Update stamina if component exists
                if (stamina != null)
                {
                    // Convert tiredness level to stamina (inverse relationship)
                    float staminaLevel = Mathf.Clamp01(1.0f - (TirednessLevel / 100.0f));
                    // Note: Using simplified approach since Stamina class may not be directly accessible
                    // stamina.SetValue(staminaLevel * stamina.GetMax());
                }

                DebugConsole.Log($"[SleepBehaviorPacket] Applied {SleepState} to duplicant {DuplicantNetId} (Tiredness: {TirednessLevel:F1}%)");
            }
            catch (Exception ex)
            {
                DebugConsole.LogException(ex);
            }
        }

        private void HandleGoingToBedState(GameObject duplicantGO, ChoreDriver choreDriver, Navigator navigator)
        {
            // Cancel current chore if not sleep-related
            var currentChore = choreDriver.GetCurrentChore();
            if (currentChore != null && currentChore.choreType.Id != "Sleep")
            {
                currentChore.Cancel("Going to bed");
            }

            // Navigate to bed position
            if (Grid.IsValidCell(Grid.PosToCell(BedPosition)))
            {
                var bedTarget = new GameObject($"BedTarget_{DuplicantNetId}");
                bedTarget.transform.position = BedPosition;
                var targetBehaviour = bedTarget.AddComponent<KMonoBehaviour>();
                
                // Clean up callback
                System.Action cleanup = () => {
                    if (bedTarget != null) UnityEngine.Object.Destroy(bedTarget);
                };
                
                navigator.Subscribe((int)GameHashes.DestinationReached, (data) => {
                    cleanup.Invoke();
                    // Once at bed, create sleep chore
                    CreateSleepChore(duplicantGO);
                });
                navigator.Subscribe((int)GameHashes.NavigationFailed, (data) => cleanup.Invoke());
                
                navigator.GoTo(targetBehaviour, new CellOffset[] { CellOffset.none });
            }
        }

        private void HandleSleepingState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.AttributeInstance stamina)
        {
            // Ensure sleep chore is active
            var currentChore = choreDriver.GetCurrentChore();
            if (currentChore == null || currentChore.choreType.Id != "Sleep")
            {
                CreateSleepChore(duplicantGO);
            }

            // Apply sleep quality effects to stamina recovery
            if (stamina != null)
            {
                float recoveryRate = SleepQuality * 0.1f; // Quality affects recovery rate
                stamina.deltaAttribute.Add(new AttributeModifier(
                    "SleepRecovery", 
                    recoveryRate, 
                    "Quality Sleep", 
                    false, 
                    false, 
                    true
                ));
            }
        }

        private void HandleWakingUpState(GameObject duplicantGO, ChoreDriver choreDriver)
        {
            // End sleep chore
            var currentChore = choreDriver.GetCurrentChore();
            if (currentChore != null && currentChore.choreType.Id == "Sleep")
            {
                currentChore.Cancel("Waking up");
            }

            // Apply wake-up effects
            var effects = duplicantGO.GetComponent<Effects>();
            if (effects != null)
            {
                effects.Remove("Tired");
                
                if (SleepQuality > 0.8f)
                {
                    effects.Add("WellRested", true);
                }
                else if (SleepQuality < 0.4f)
                {
                    effects.Add("PoorSleep", true);
                }
            }
        }

        private void HandleTiredState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.AttributeInstance stamina)
        {
            // Apply tired effects
            var effects = duplicantGO.GetComponent<Effects>();
            if (effects != null)
            {
                effects.Add("Tired", true);
            }

            // Reduce work efficiency when tired
            if (stamina != null && TirednessLevel > 80.0f)
            {
                var attributes = duplicantGO.GetComponent<AttributeModifiers>();
                if (attributes != null)
                {
                    attributes.Add(new AttributeModifier(
                        Db.Get().Attributes.Digging.Id,
                        -0.2f,
                        "Exhausted",
                        false,
                        false,
                        true
                    ));
                }
            }
        }

        private void HandleRestedState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.AttributeInstance stamina)
        {
            // Remove tired effects and add rested bonuses
            var effects = duplicantGO.GetComponent<Effects>();
            if (effects != null)
            {
                effects.Remove("Tired");
                effects.Add("WellRested", true);
            }

            // Restore full stamina
            if (stamina != null)
            {
                stamina.SetValue(stamina.GetMax());
            }
        }

        private void CreateSleepChore(GameObject duplicantGO)
        {
            var consumer = duplicantGO.GetComponent<ChoreConsumer>();
            if (consumer != null)
            {
                var sleepChore = ONI_MP.Misc.ChoreFactory.Create("Sleep", 
                    new Chore.Precondition.Context(), duplicantGO, BedPosition, Grid.PosToCell(BedPosition), "");
                
                if (sleepChore != null)
                {
                    var context = new Chore.Precondition.Context(sleepChore, new ChoreConsumerState(consumer), true);
                    sleepChore.Begin(context);
                }
            }
        }
    }
}
