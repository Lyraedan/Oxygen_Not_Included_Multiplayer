using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Synchronizes general duplicant behavior states and efficiency modifiers.
    /// Handles eating, working, and movement behaviors with performance tracking.
    /// </summary>
    public class BehaviorStatePacket : IPacket
    {
        public int DuplicantNetId;
        public string CurrentBehavior;      // "Working", "Eating", "Moving", "Idle"
        public string BehaviorSubtype;      // "Focused", "Distracted", "Fast", "Slow", etc.
        public Vector3 BehaviorLocation;
        public float BehaviorProgress;      // Progress of current behavior (0.0-1.0)
        public float EfficiencyModifier;    // 0.0-2.0, 1.0 is normal
        public string TargetObject;        // What they're interacting with
        public float BehaviorDuration;      // How long this behavior will last
        public bool IsInterruptible;       // Whether this behavior can be interrupted

        public PacketType Type => PacketType.BehaviorState;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(CurrentBehavior ?? "");
            writer.Write(BehaviorSubtype ?? "");
            writer.Write(BehaviorLocation.x);
            writer.Write(BehaviorLocation.y);
            writer.Write(BehaviorLocation.z);
            writer.Write(BehaviorProgress);
            writer.Write(EfficiencyModifier);
            writer.Write(TargetObject ?? "");
            writer.Write(BehaviorDuration);
            writer.Write(IsInterruptible);
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            CurrentBehavior = reader.ReadString();
            BehaviorSubtype = reader.ReadString();
            BehaviorLocation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            BehaviorProgress = reader.ReadSingle();
            EfficiencyModifier = reader.ReadSingle();
            TargetObject = reader.ReadString();
            BehaviorDuration = reader.ReadSingle();
            IsInterruptible = reader.ReadBoolean();
        }

        public bool IsValid()
        {
            return DuplicantNetId > 0 && !string.IsNullOrEmpty(CurrentBehavior);
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantNetId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"BehaviorStatePacket: Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var choreDriver = duplicantGO.GetComponent<ChoreDriver>();
            if (choreDriver == null)
            {
                Debug.LogWarning($"BehaviorStatePacket: Duplicant {DuplicantNetId} has no ChoreDriver");
                return;
            }

            var effects = duplicantGO.GetComponent<Klei.AI.Effects>();

            switch (CurrentBehavior)
            {
                case "Working":
                    HandleWorkingBehavior(duplicantGO, choreDriver, effects);
                    break;
                case "Eating":
                    HandleEatingBehavior(duplicantGO, choreDriver, effects);
                    break;
                case "Moving":
                    HandleMovingBehavior(duplicantGO, choreDriver, effects);
                    break;
                case "Idle":
                    HandleIdleBehavior(duplicantGO, choreDriver, effects);
                    break;
                default:
                    Debug.LogWarning($"Unknown behavior type: {CurrentBehavior}");
                    break;
            }

            // Apply efficiency modifiers
            ApplyEfficiencyModifiers(duplicantGO, effects);

            Debug.Log($"Applied behavior state {CurrentBehavior}:{BehaviorSubtype} to duplicant {DuplicantNetId} (efficiency: {EfficiencyModifier:F2})");
        }

        private void HandleWorkingBehavior(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.Effects effects)
        {
            // Apply work-related behavior modifiers
            try
            {
                if (effects != null)
                {
                    switch (BehaviorSubtype)
                    {
                        case "Focused":
                            effects.Add("WorkFocused", true);
                            break;
                        case "Distracted":
                            effects.Add("WorkDistracted", true);
                            break;
                        case "Efficient":
                            effects.Add("WorkEfficient", true);
                            break;
                    }
                }
                
                Debug.Log($"Applied working behavior {BehaviorSubtype} to duplicant");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to apply working behavior: {ex.Message}");
            }
        }

        private void HandleEatingBehavior(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.Effects effects)
        {
            // Handle eating behavior
            var consumer = duplicantGO.GetComponent<ChoreConsumer>();
            if (consumer != null)
            {
                try
                {
                    // Look for active eating chore
                    var currentChore = consumer.choreDriver?.GetCurrentChore();
                    if (currentChore != null && currentChore.GetType().Name.Contains("Eat"))
                    {
                        Debug.Log($"Duplicant {DuplicantNetId} is eating: {BehaviorSubtype}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to handle eating behavior: {ex.Message}");
                }
            }

            // Simplified eating effect application
            if (effects != null)
            {
                try
                {
                    switch (BehaviorSubtype)
                    {
                        case "GoodFood":
                            effects.Add("GoodFood", true);
                            break;
                        case "BadFood":
                            effects.Add("BadFood", true);
                            break;
                        case "GreatFood":
                            effects.Add("GreatFood", true);
                            break;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to apply eating effects: {ex.Message}");
                }
            }
        }

        private void HandleMovingBehavior(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.Effects effects)
        {
            // Handle movement behavior
            var navigator = duplicantGO.GetComponent<Navigator>();
            if (navigator != null)
            {
                try
                {
                    // Apply movement modifiers based on efficiency
                    Debug.Log($"Duplicant {DuplicantNetId} moving with {BehaviorSubtype} behavior (efficiency: {EfficiencyModifier:F2})");
                    
                    // Move to behavior position if specified
                    if (BehaviorLocation != Vector3.zero)
                    {
                        int targetCell = Grid.PosToCell(BehaviorLocation);
                        if (Grid.IsValidCell(targetCell))
                        {
                            navigator.GoTo(targetCell);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to handle movement behavior: {ex.Message}");
                }
            }
        }

        private void HandleIdleBehavior(GameObject duplicantGO, ChoreDriver choreDriver, Klei.AI.Effects effects)
        {
            // Handle idle behavior state
            try
            {
                Debug.Log($"Duplicant {DuplicantNetId} in idle behavior: {BehaviorSubtype}");
                
                if (effects != null)
                {
                    switch (BehaviorSubtype)
                    {
                        case "Relaxed":
                            effects.Add("Relaxed", true);
                            break;
                        case "Bored":
                            effects.Add("Bored", true);
                            break;
                        case "Content":
                            effects.Add("Content", true);
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to handle idle behavior: {ex.Message}");
            }
        }

        private void ApplyEfficiencyModifiers(GameObject duplicantGO, Klei.AI.Effects effects)
        {
            // Simplified efficiency application using effects only
            if (effects == null) return;

            try
            {
                // Apply behavior-specific effects based on efficiency
                if (EfficiencyModifier > 1.2f)
                {
                    effects.Add("HighEfficiency", true);
                }
                else if (EfficiencyModifier < 0.8f)
                {
                    effects.Add("LowEfficiency", true);
                }
                
                Debug.Log($"Applied efficiency modifiers for {CurrentBehavior} - {BehaviorSubtype} (efficiency: {EfficiencyModifier:F2})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to apply efficiency modifiers: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"BehaviorStatePacket[DuplicantNetId={DuplicantNetId}, Type={CurrentBehavior}, Subtype={BehaviorSubtype}, Efficiency={EfficiencyModifier:F2}]";
        }

        public void OnDispatched()
        {
            // Called when packet is sent - cleanup if needed
        }
    }
}
