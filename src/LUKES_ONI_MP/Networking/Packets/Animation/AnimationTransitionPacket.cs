using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Animation
{
    /// <summary>
    /// Animation transition packet for smooth animation state changes.
    /// Handles transitions between different animations to prevent abrupt changes.
    /// Essential for natural duplicant movement and state changes.
    /// </summary>
    public class AnimationTransitionPacket : IPacket
    {
        public int DuplicantNetId;
        public string FromAnimation;         // Animation transitioning from
        public string ToAnimation;           // Animation transitioning to
        public float TransitionDuration;     // Duration of transition in seconds
        public float TransitionProgress;     // Current transition progress (0.0 to 1.0)
        public TransitionType TransitionMode;   // Type of transition
        public float BlendFactor;            // Blend factor between animations
        public bool IsImmediate;             // Whether transition should be immediate
        public float FromFrame;              // Frame position in source animation
        public float ToFrame;                // Frame position in target animation
        public string TransitionContext;     // Context: "Movement", "Work", "Idle", etc.
        public Vector3 MovementDirection;    // Direction of movement during transition
        public DateTime TransitionStartTime; // When transition started

        public PacketType Type => PacketType.AnimationTransition;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(FromAnimation ?? "");
            writer.Write(ToAnimation ?? "");
            writer.Write(TransitionDuration);
            writer.Write(TransitionProgress);
            writer.Write((int)TransitionMode);
            writer.Write(BlendFactor);
            writer.Write(IsImmediate);
            writer.Write(FromFrame);
            writer.Write(ToFrame);
            writer.Write(TransitionContext ?? "");
            
            writer.Write(MovementDirection.x);
            writer.Write(MovementDirection.y);
            writer.Write(MovementDirection.z);
            
            writer.Write(DateTime.UtcNow.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            FromAnimation = reader.ReadString();
            ToAnimation = reader.ReadString();
            TransitionDuration = reader.ReadSingle();
            TransitionProgress = reader.ReadSingle();
            TransitionMode = (TransitionType)reader.ReadInt32();
            BlendFactor = reader.ReadSingle();
            IsImmediate = reader.ReadBoolean();
            FromFrame = reader.ReadSingle();
            ToFrame = reader.ReadSingle();
            TransitionContext = reader.ReadString();
            
            MovementDirection = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            TransitionStartTime = DateTime.FromBinary(reader.ReadInt64());
        }

        public bool IsValid()
        {
            return DuplicantNetId > 0 && 
                   !string.IsNullOrEmpty(ToAnimation) && 
                   TransitionProgress >= 0f && 
                   TransitionProgress <= 1f;
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantNetId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"AnimationTransitionPacket: Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var animController = duplicantGO.GetComponent<KAnimControllerBase>();
            if (animController == null)
            {
                Debug.LogWarning($"AnimationTransitionPacket: Duplicant {DuplicantNetId} has no animation controller");
                return;
            }

            try
            {
                ApplyTransition(animController);
                
                DebugConsole.Log($"[AnimationTransition] Applied transition from {FromAnimation} to {ToAnimation} " +
                               $"for duplicant {DuplicantNetId} (Progress: {TransitionProgress:F2}, Type: {TransitionMode})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply animation transition for duplicant {DuplicantNetId}: {ex.Message}");
            }
        }

        private void ApplyTransition(KAnimControllerBase animController)
        {
            if (IsImmediate)
            {
                ApplyImmediateTransition(animController);
            }
            else
            {
                ApplyProgressiveTransition(animController);
            }
        }

        private void ApplyImmediateTransition(KAnimControllerBase animController)
        {
            try
            {
                // Immediately switch to target animation
                var playMode = DeterminePlayMode();
                animController.Play(ToAnimation, playMode, 1f, 0f);
                
                // Set target frame if specified
                if (ToFrame > 0f)
                {
                    animController.SetPositionPercent(ToFrame);
                }
                
                DebugConsole.Log($"Immediate transition to {ToAnimation} for duplicant {DuplicantNetId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed immediate transition: {ex.Message}");
            }
        }

        private void ApplyProgressiveTransition(KAnimControllerBase animController)
        {
            try
            {
                switch (TransitionMode)
                {
                    case TransitionType.Crossfade:
                        ApplyCrossfadeTransition(animController);
                        break;
                    case TransitionType.Sequential:
                        ApplySequentialTransition(animController);
                        break;
                    case TransitionType.Blend:
                        ApplyBlendTransition(animController);
                        break;
                    case TransitionType.Interrupt:
                        ApplyInterruptTransition(animController);
                        break;
                    default:
                        ApplyDefaultTransition(animController);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed progressive transition: {ex.Message}");
            }
        }

        private void ApplyCrossfadeTransition(KAnimControllerBase animController)
        {
            // Crossfade between animations based on progress
            if (TransitionProgress < 0.5f)
            {
                // First half: play from animation with decreasing opacity
                animController.Play(FromAnimation, KAnim.PlayMode.Once, 1f, 0f);
                var opacity = 1f - (TransitionProgress * 2f);
                SetAnimationOpacity(animController, opacity);
            }
            else
            {
                // Second half: play to animation with increasing opacity
                animController.Play(ToAnimation, DeterminePlayMode(), 1f, 0f);
                var opacity = (TransitionProgress - 0.5f) * 2f;
                SetAnimationOpacity(animController, opacity);
            }
        }

        private void ApplySequentialTransition(KAnimControllerBase animController)
        {
            // Play animations in sequence
            if (TransitionProgress < 1f)
            {
                // Still playing from animation
                animController.Play(FromAnimation, KAnim.PlayMode.Once, 1f, 0f);
                var fromProgress = FromFrame + (TransitionProgress * (1f - FromFrame));
                animController.SetPositionPercent(fromProgress);
            }
            else
            {
                // Switch to target animation
                animController.Play(ToAnimation, DeterminePlayMode(), 1f, 0f);
                animController.SetPositionPercent(ToFrame);
            }
        }

        private void ApplyBlendTransition(KAnimControllerBase animController)
        {
            // Blend animations based on blend factor
            animController.Play(ToAnimation, DeterminePlayMode(), 1f, 0f);
            
            // Apply blending effect (simplified version)
            var blendedFrame = Mathf.Lerp(FromFrame, ToFrame, BlendFactor);
            animController.SetPositionPercent(blendedFrame);
        }

        private void ApplyInterruptTransition(KAnimControllerBase animController)
        {
            // Interrupt current animation and start new one
            animController.Stop();
            animController.Play(ToAnimation, DeterminePlayMode(), 1f, 0f);
            
            if (ToFrame > 0f)
            {
                animController.SetPositionPercent(ToFrame);
            }
        }

        private void ApplyDefaultTransition(KAnimControllerBase animController)
        {
            // Default smooth transition
            var playMode = DeterminePlayMode();
            animController.Play(ToAnimation, playMode, 1f, 0f);
            
            // Interpolate frame position
            var targetFrame = Mathf.Lerp(FromFrame, ToFrame, TransitionProgress);
            animController.SetPositionPercent(targetFrame);
        }

        private KAnim.PlayMode DeterminePlayMode()
        {
            // Determine play mode based on animation and context
            switch (TransitionContext)
            {
                case "Movement":
                    return IsLoopingAnimation(ToAnimation) ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once;
                case "Work":
                    return KAnim.PlayMode.Loop;
                case "Idle":
                    return KAnim.PlayMode.Loop;
                case "Climbing":
                    return KAnim.PlayMode.Loop;
                default:
                    return IsLoopingAnimation(ToAnimation) ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once;
            }
        }

        private bool IsLoopingAnimation(string animName)
        {
            // Check if animation should loop based on name
            return animName.Contains("loop") || 
                   animName.Contains("walk") || 
                   animName.Contains("run") || 
                   animName.Contains("idle") || 
                   animName.Contains("work") || 
                   animName.Contains("climb");
        }

        private void SetAnimationOpacity(KAnimControllerBase animController, float opacity)
        {
            try
            {
                // Try to set animation opacity for fade effects
                if (animController is KBatchedAnimController batchedController)
                {
                    var tintProperty = batchedController.GetType()
                        .GetProperty("TintColour", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (tintProperty != null && tintProperty.CanWrite)
                    {
                        var currentTint = (Color)tintProperty.GetValue(batchedController);
                        currentTint.a = opacity;
                        tintProperty.SetValue(batchedController, currentTint);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to set animation opacity: {ex.Message}");
            }
        }

        public void OnDispatched()
        {
            // Called after packet is sent
        }

        public override string ToString()
        {
            return $"AnimationTransitionPacket[DuplicantNetId={DuplicantNetId}, " +
                   $"From={FromAnimation}, To={ToAnimation}, Progress={TransitionProgress:F2}, Type={TransitionMode}]";
        }
    }

    /// <summary>
    /// Types of animation transitions
    /// </summary>
    public enum TransitionType
    {
        Crossfade,    // Fade between animations
        Sequential,   // Play animations in sequence
        Blend,        // Blend animations together
        Interrupt     // Interrupt and switch immediately
    }
}
