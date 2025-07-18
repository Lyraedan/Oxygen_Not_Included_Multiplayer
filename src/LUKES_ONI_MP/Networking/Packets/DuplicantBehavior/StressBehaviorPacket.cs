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
    /// Synchronizes duplicant stress behaviors including emotional outbursts and mood changes.
    /// Coordinates stress responses, breakdowns, and recovery across multiplayer clients.
    /// </summary>
    public class StressBehaviorPacket : IPacket
    {
        public int DuplicantNetId;
        public string StressState;          // "Calm", "Stressed", "StressedOut", "Breakdown", "Recovery"
        public float StressLevel;           // Current stress percentage (0-100)
        public string StressReaction;       // "BingeEating", "DestructiveBehavior", "UglyCrying", "Aggressive", "Vomiting"
        public Vector3 StressLocation;      // Where the stress behavior is occurring
        public float StressDuration;        // How long this stress state has been active
        public List<string> StressCauses;   // Reasons for stress: "Noise", "Heat", "Cold", "BadFood", "Isolation"
        public string MoodState;            // "Happy", "Neutral", "Unhappy", "Miserable", "Elated"
        public float MoraleLevel;           // Current morale (can be negative)
        public bool IsStressBreakdown;      // Is this a full stress breakdown?
        public string StressEmote;          // Current stress emote being displayed
        public DateTime StressStartTime;

        public PacketType Type => PacketType.StressBehavior;

        public StressBehaviorPacket()
        {
            StressCauses = new List<string>();
            StressStartTime = DateTime.UtcNow;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(StressState ?? "");
            writer.Write(StressLevel);
            writer.Write(StressReaction ?? "");
            
            writer.Write(StressLocation.x);
            writer.Write(StressLocation.y);
            writer.Write(StressLocation.z);
            
            writer.Write(StressDuration);
            
            writer.Write(StressCauses.Count);
            foreach (var cause in StressCauses)
            {
                writer.Write(cause ?? "");
            }
            
            writer.Write(MoodState ?? "");
            writer.Write(MoraleLevel);
            writer.Write(IsStressBreakdown);
            writer.Write(StressEmote ?? "");
            writer.Write(StressStartTime.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            StressState = reader.ReadString();
            StressLevel = reader.ReadSingle();
            StressReaction = reader.ReadString();
            
            StressLocation = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            StressDuration = reader.ReadSingle();
            
            StressCauses.Clear();
            int causeCount = reader.ReadInt32();
            for (int i = 0; i < causeCount; i++)
            {
                StressCauses.Add(reader.ReadString());
            }
            
            MoodState = reader.ReadString();
            MoraleLevel = reader.ReadSingle();
            IsStressBreakdown = reader.ReadBoolean();
            StressEmote = reader.ReadString();
            StressStartTime = DateTime.FromBinary(reader.ReadInt64());
        }

        public void OnDispatched()
        {
            if (!NetworkIdentityRegistry.TryGet(DuplicantNetId, out var entity))
            {
                DebugConsole.LogWarning($"[StressBehaviorPacket] Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var duplicantGO = entity.gameObject;
            var choreDriver = duplicantGO.GetComponent<ChoreDriver>();
            var stress = duplicantGO.GetComponent<AttributeInstance>();
                var effects = duplicantGO.GetComponent<Klei.AI.Effects>();            if (choreDriver == null)
            {
                DebugConsole.LogWarning($"[StressBehaviorPacket] Duplicant {DuplicantNetId} missing required components");
                return;
            }

            try
            {
                // Update stress level
                UpdateStressAttributes(duplicantGO);
                
                // Handle different stress states
                switch (StressState)
                {
                    case "Calm":
                        HandleCalmState(duplicantGO, choreDriver, effects);
                        break;
                        
                    case "Stressed":
                        HandleStressedState(duplicantGO, choreDriver, effects);
                        break;
                        
                    case "StressedOut":
                        HandleStressedOutState(duplicantGO, choreDriver, effects);
                        break;
                        
                    case "Breakdown":
                        HandleBreakdownState(duplicantGO, choreDriver, effects);
                        break;
                        
                    case "Recovery":
                        HandleRecoveryState(duplicantGO, choreDriver, effects);
                        break;
                        
                    default:
                        DebugConsole.LogWarning($"[StressBehaviorPacket] Unknown stress state: {StressState}");
                        break;
                }

                // Apply stress reaction if any
                if (!string.IsNullOrEmpty(StressReaction))
                {
                    ApplyStressReaction(duplicantGO, choreDriver);
                }

                // Apply stress emote
                if (!string.IsNullOrEmpty(StressEmote))
                {
                    ApplyStressEmote(duplicantGO);
                }

                DebugConsole.Log($"[StressBehaviorPacket] Applied {StressState} to duplicant {DuplicantNetId} (Stress: {StressLevel:F1}%, Morale: {MoraleLevel:F1})");
            }
            catch (Exception ex)
            {
                DebugConsole.LogException(ex);
            }
        }

        private void UpdateStressAttributes(GameObject duplicantGO)
        {
            // Update stress attribute
            var attributes = duplicantGO.GetComponent<Klei.AI.Attributes>();
            if (attributes != null)
            {
                var stressAttribute = attributes.Get(Db.Get().Attributes.QualityOfLife.Id);
                if (stressAttribute != null)
                {
                    // ONI stress is inverse - high QoL = low stress
                    float qualityOfLife = Mathf.Clamp(100.0f - StressLevel, 0.0f, 100.0f);
                    stressAttribute.SetValue(qualityOfLife);
                }

                var moraleAttribute = attributes.Get(Db.Get().Attributes.QualityOfLifeExpectation.Id);
                if (moraleAttribute != null)
                {
                    moraleAttribute.SetValue(MoraleLevel);
                }
            }
        }

        private void HandleCalmState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.Effects effects)
        {
            // Remove stress effects
            if (effects != null)
            {
                effects.Remove("Stressed");
                effects.Remove("StressedOut");
                effects.Remove("Aggressive");
            }

            // Cancel stress-related chores
            var currentChore = choreDriver.GetCurrentChore();
            if (currentChore != null && IsStressChore(currentChore.choreType.Id))
            {
                currentChore.Cancel("Stress resolved");
            }
        }

        private void HandleStressedState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.Effects effects)
        {
            // Apply stressed effects
            if (effects != null)
            {
                effects.Add("Stressed", true);
                effects.Remove("StressedOut"); // Remove higher stress levels
            }

            // Apply stress causes as debuffs
            ApplyStressCauseEffects(duplicantGO, effects);
        }

        private void HandleStressedOutState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.Effects effects)
        {
            // Apply stressed out effects
            if (effects != null)
            {
                effects.Add("StressedOut", true);
                effects.Remove("Stressed"); // Replace with higher stress level
            }

            // Higher chance of stress reactions
            if (UnityEngine.Random.value < 0.3f && !string.IsNullOrEmpty(StressReaction))
            {
                ApplyStressReaction(duplicantGO, choreDriver);
            }
        }

        private void HandleBreakdownState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.Effects effects)
        {
            // Cancel current work and force stress behavior
            var currentChore = choreDriver.GetCurrentChore();
            if (currentChore != null && !IsStressChore(currentChore.choreType.Id))
            {
                currentChore.Cancel("Stress breakdown");
            }

            // Apply breakdown effects
            if (effects != null)
            {
                effects.Add("StressedOut", true);
                if (!string.IsNullOrEmpty(StressReaction))
                {
                    effects.Add(GetStressReactionEffect(StressReaction), true);
                }
            }

            // Force stress reaction chore
            ApplyStressReaction(duplicantGO, choreDriver);
        }

        private void HandleRecoveryState(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.Effects effects)
        {
            // Gradually remove stress effects
            if (effects != null)
            {
                effects.Remove("StressedOut");
                if (StressLevel < 50.0f)
                {
                    effects.Remove("Stressed");
                }
            }

            // Cancel stress chores if stress is low enough
            if (StressLevel < 25.0f)
            {
                var currentChore = choreDriver.GetCurrentChore();
                if (currentChore != null && IsStressChore(currentChore.choreType.Id))
                {
                    currentChore.Cancel("Stress recovery");
                }
            }
        }

        private void ApplyStressReaction(GameObject duplicantGO, ChoreDriver choreDriver)
        {
            var consumer = duplicantGO.GetComponent<ChoreConsumer>();
            if (consumer == null) return;

            Chore stressChore = null;
            
            switch (StressReaction)
            {
                case "BingeEating":
                    stressChore = ONI_MP.Misc.ChoreFactory.Create("BingeEat", 
                        new Chore.Precondition.Context(), duplicantGO, StressLocation, Grid.PosToCell(StressLocation), "");
                    break;
                    
                case "DestructiveBehavior":
                    stressChore = ONI_MP.Misc.ChoreFactory.Create("Aggressive", 
                        new Chore.Precondition.Context(), duplicantGO, StressLocation, Grid.PosToCell(StressLocation), "");
                    break;
                    
                case "UglyCrying":
                    stressChore = ONI_MP.Misc.ChoreFactory.Create("UglyCry", 
                        new Chore.Precondition.Context(), duplicantGO, StressLocation, Grid.PosToCell(StressLocation), "");
                    break;
                    
                case "Vomiting":
                    stressChore = ONI_MP.Misc.ChoreFactory.Create("Vomit", 
                        new Chore.Precondition.Context(), duplicantGO, StressLocation, Grid.PosToCell(StressLocation), "");
                    break;
                    
                case "Aggressive":
                    stressChore = ONI_MP.Misc.ChoreFactory.Create("Aggressive", 
                        new Chore.Precondition.Context(), duplicantGO, StressLocation, Grid.PosToCell(StressLocation), "");
                    break;
            }

            if (stressChore != null)
            {
                var context = new Chore.Precondition.Context(stressChore, new ChoreConsumerState(consumer), true);
                stressChore.Begin(context);
            }
        }

        private void ApplyStressCauseEffects(GameObject duplicantGO, Klei.AI.Effects effects)
        {
            if (effects == null) return;

            foreach (var cause in StressCauses)
            {
                switch (cause)
                {
                    case "Noise":
                        effects.Add("NoisePollution", true);
                        break;
                    case "Heat":
                        effects.Add("Hot", true);
                        break;
                    case "Cold":
                        effects.Add("Cold", true);
                        break;
                    case "BadFood":
                        effects.Add("BadFood", true);
                        break;
                    case "Isolation":
                        effects.Add("Alone", true);
                        break;
                }
            }
        }

        private void ApplyStressEmote(GameObject duplicantGO)
        {
            var emoteChore = duplicantGO.GetComponent<EmoteChore>();
            if (emoteChore != null)
            {
                // Try to trigger the stress emote
                var consumer = duplicantGO.GetComponent<ChoreConsumer>();
                if (consumer != null)
                {
                    var stressEmoteChore = ONI_MP.Misc.ChoreFactory.Create("StressEmote", 
                        new Chore.Precondition.Context(), duplicantGO, duplicantGO.transform.position, 
                        Grid.PosToCell(duplicantGO), "");
                    
                    if (stressEmoteChore != null)
                    {
                        var context = new Chore.Precondition.Context(stressEmoteChore, new ChoreConsumerState(consumer), true);
                        stressEmoteChore.Begin(context);
                    }
                }
            }
        }

        private bool IsStressChore(string choreTypeId)
        {
            return choreTypeId == "BingeEat" || choreTypeId == "Aggressive" || 
                   choreTypeId == "UglyCry" || choreTypeId == "Vomit" || 
                   choreTypeId == "StressEmote" || choreTypeId == "StressIdle";
        }

        private string GetStressReactionEffect(string reaction)
        {
            switch (reaction)
            {
                case "BingeEating": return "BingeEating";
                case "DestructiveBehavior": return "Aggressive";
                case "UglyCrying": return "Crying";
                case "Vomiting": return "Nauseous";
                case "Aggressive": return "Aggressive";
                default: return "Stressed";
            }
        }
    }
}
