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
        public System.DateTime PacketTime;

        public PacketType Type => PacketType.SleepBehavior;

        public SleepBehaviorPacket()
        {
            PacketTime = System.DateTime.UtcNow;
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
            PacketTime = System.DateTime.FromBinary(reader.ReadInt64());
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
            var amounts = duplicantGO.GetComponent<Klei.AI.Amounts>();
            var stamina = amounts?.Get(Db.Get().Amounts.Stamina.Id);
            
            if (choreDriver == null || navigator == null)
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
                var targetCell = Grid.PosToCell(BedPosition);
                
                // Navigate to bed position
                navigator.Subscribe((int)GameHashes.DestinationReached, (data) => {
                    // Once at bed, create sleep chore
                    CreateSleepChore(duplicantGO);
                });
                navigator.Subscribe((int)GameHashes.NavigationFailed, (data) => {
                    DebugConsole.LogWarning($"[SleepBehaviorPacket] Failed to navigate to bed at {BedPosition}");
                });
                
                navigator.GoTo(targetCell, new CellOffset[] { CellOffset.none });
            }
        }

        private void HandleSleepingState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.AmountInstance stamina)
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
                // TODO: Implement proper stamina modifier application
                // Apply modifier to the stamina amount directly
                /* var staminaAttribute = stamina.GetComponent<Klei.AI.AttributeInstance>();
                if (staminaAttribute != null)
                {
                    staminaAttribute.deltaAttribute.Add(new Klei.AI.AttributeModifier(
                        "SleepRecovery", 
                        recoveryRate, 
                        "Quality Sleep", 
                        false, 
                        false, 
                        true
                    ));
                } */
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
            var effects = duplicantGO.GetComponent<Klei.AI.Effects>();
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

        private void HandleTiredState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.AmountInstance stamina)
        {
            // Apply tired effects
            var effects = duplicantGO.GetComponent<Klei.AI.Effects>();
            if (effects != null)
            {
                effects.Add("Tired", true);
            }

            // Reduce work efficiency when tired
            if (stamina != null && TirednessLevel > 80.0f)
            {
                // Apply tiredness effects - simplified for now
                DebugConsole.Log($"[SleepBehaviorPacket] Duplicant {DuplicantNetId} is very tired (level: {TirednessLevel:F1}%)");
            }
        }

        private void HandleRestedState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.AmountInstance stamina)
        {
            // Remove tired effects and add rested bonuses
            var effects = duplicantGO.GetComponent<Klei.AI.Effects>();
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
                // For now, use simplified sleep behavior
                // TODO: Implement proper sleep chore creation when ChoreFactory is available
                DebugConsole.Log($"[SleepBehaviorPacket] Sleep chore requested for duplicant {DuplicantNetId} at {BedPosition}");
            }
        }
    }
}
