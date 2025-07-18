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
        public string BehaviorType;         // "Working", "Eating", "Moving", "Idle"
        public string BehaviorSubtype;      // "Focused", "Distracted", "Fast", "Slow", etc.
        public Vector3 BehaviorPosition;
        public float BehaviorEfficiency;    // 0.0-2.0, 1.0 is normal
        public string TargetObject;        // What they're interacting with
        public float BehaviorDuration;      // How long this behavior will last
        public bool IsInterruptible;       // Whether this behavior can be interrupted

        public PacketType Type => PacketType.BehaviorState;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(BehaviorType ?? "");
            writer.Write(BehaviorSubtype ?? "");
            writer.Write(BehaviorPosition.x);
            writer.Write(BehaviorPosition.y);
            writer.Write(BehaviorPosition.z);
            writer.Write(BehaviorEfficiency);
            writer.Write(TargetObject ?? "");
            writer.Write(BehaviorDuration);
            writer.Write(IsInterruptible);
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            BehaviorType = reader.ReadString();
            BehaviorSubtype = reader.ReadString();
            BehaviorPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            BehaviorEfficiency = reader.ReadSingle();
            TargetObject = reader.ReadString();
            BehaviorDuration = reader.ReadSingle();
            IsInterruptible = reader.ReadBoolean();
        }

        public bool IsValid()
        {
            return DuplicantNetId > 0 && !string.IsNullOrEmpty(BehaviorType);
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

            switch (BehaviorType)
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
                    Debug.LogWarning($"Unknown behavior type: {BehaviorType}");
                    break;
            }

            // Apply efficiency modifiers
            ApplyEfficiencyModifiers(duplicantGO, effects);

            Debug.Log($"Applied behavior state {BehaviorType}:{BehaviorSubtype} to duplicant {DuplicantNetId} (efficiency: {BehaviorEfficiency:F2})");
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
                    Debug.Log($"Duplicant {DuplicantNetId} moving with {BehaviorSubtype} behavior (efficiency: {BehaviorEfficiency:F2})");
                    
                    // Move to behavior position if specified
                    if (BehaviorPosition != Vector3.zero)
                    {
                        int targetCell = Grid.PosToCell(BehaviorPosition);
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
                if (BehaviorEfficiency > 1.2f)
                {
                    effects.Add("HighEfficiency", true);
                }
                else if (BehaviorEfficiency < 0.8f)
                {
                    effects.Add("LowEfficiency", true);
                }
                
                Debug.Log($"Applied efficiency modifiers for {BehaviorType} - {BehaviorSubtype} (efficiency: {BehaviorEfficiency:F2})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to apply efficiency modifiers: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"BehaviorStatePacket[DuplicantNetId={DuplicantNetId}, Type={BehaviorType}, Subtype={BehaviorSubtype}, Efficiency={BehaviorEfficiency:F2}]";
        }

        public void OnDispatched()
        {
            // Called when packet is sent - cleanup if needed
        }
    }
}
