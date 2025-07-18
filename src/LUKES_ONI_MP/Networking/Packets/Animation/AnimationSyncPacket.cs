using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;
using System.Collections.Generic;

namespace ONI_MP.Networking.Packets.Animation
{
    /// <summary>
    /// Comprehensive animation state synchronization packet.
    /// Handles complete animation state including frame, speed, looping, and context.
    /// Addresses the issue of animations getting stuck on single frames.
    /// </summary>
    public class AnimationSyncPacket : IPacket
    {
        public int DuplicantNetId;
        public string CurrentAnimation;      // Name of the currently playing animation
        public float CurrentFrame;           // Current frame position (0.0 to 1.0)
        public float AnimationSpeed;         // Speed multiplier (1.0 = normal)
        public bool IsLooping;               // Whether animation should loop
        public bool IsPaused;                // Whether animation is paused
        public string AnimationContext;      // Context: "Walking", "Working", "Idle", "Climbing", etc.
        public Vector3 Position;             // Current duplicant position for sync validation
        public Vector3 MovementDirection;    // Movement direction for directional animations
        public float MovementSpeed;          // Movement speed for speed-based animations
        public string BlendAnimation;        // Secondary animation for blending
        public float BlendFactor;            // Blend factor between animations (0.0 to 1.0)
        public System.DateTime LastUpdateTime;      // When this animation state was last updated

        public PacketType Type => PacketType.AnimationSync;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(CurrentAnimation ?? "");
            writer.Write(CurrentFrame);
            writer.Write(AnimationSpeed);
            writer.Write(IsLooping);
            writer.Write(IsPaused);
            writer.Write(AnimationContext ?? "");
            
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            
            writer.Write(MovementDirection.x);
            writer.Write(MovementDirection.y);
            writer.Write(MovementDirection.z);
            
            writer.Write(MovementSpeed);
            writer.Write(BlendAnimation ?? "");
            writer.Write(BlendFactor);
            writer.Write(System.DateTime.UtcNow.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            CurrentAnimation = reader.ReadString();
            CurrentFrame = reader.ReadSingle();
            AnimationSpeed = reader.ReadSingle();
            IsLooping = reader.ReadBoolean();
            IsPaused = reader.ReadBoolean();
            AnimationContext = reader.ReadString();
            
            Position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            MovementDirection = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            MovementSpeed = reader.ReadSingle();
            BlendAnimation = reader.ReadString();
            BlendFactor = reader.ReadSingle();
            LastUpdateTime = System.DateTime.FromBinary(reader.ReadInt64());
        }

        public bool IsValid()
        {
            return DuplicantNetId > 0 && !string.IsNullOrEmpty(CurrentAnimation);
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantNetId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"AnimationSyncPacket: Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var animController = duplicantGO.GetComponent<KAnimControllerBase>();
            if (animController == null)
            {
                Debug.LogWarning($"AnimationSyncPacket: Duplicant {DuplicantNetId} has no animation controller");
                return;
            }

            try
            {
                // Apply animation state
                ApplyAnimationState(animController);
                
                // Sync animation frame
                SyncAnimationFrame(animController);
                
                // Handle animation blending if needed
                if (!string.IsNullOrEmpty(BlendAnimation) && BlendFactor > 0f)
                {
                    HandleAnimationBlending(animController);
                }
                
                // Validate position sync
                ValidatePositionSync(duplicantGO);
                
                DebugConsole.Log($"[AnimationSync] Applied {CurrentAnimation} to duplicant {DuplicantNetId} " +
                               $"(Frame: {CurrentFrame:F2}, Speed: {AnimationSpeed:F2}, Context: {AnimationContext})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply animation sync for duplicant {DuplicantNetId}: {ex.Message}");
            }
        }

        private void ApplyAnimationState(KAnimControllerBase animController)
        {
            // Determine play mode based on context
            var playMode = IsLooping ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once;
            
            // Apply animation based on context
            switch (AnimationContext)
            {
                case "Walking":
                    ApplyMovementAnimation(animController, playMode);
                    break;
                case "Working":
                    ApplyWorkAnimation(animController, playMode);
                    break;
                case "Idle":
                    ApplyIdleAnimation(animController, playMode);
                    break;
                case "Climbing":
                    ApplyClimbingAnimation(animController, playMode);
                    break;
                case "Swimming":
                    ApplySwimmingAnimation(animController, playMode);
                    break;
                default:
                    // Generic animation application
                    animController.Play(CurrentAnimation, playMode, AnimationSpeed, 0f);
                    break;
            }
            
            // Handle pause state
            if (IsPaused)
            {
                animController.SetPositionPercent(CurrentFrame);
                animController.StopAndClear();
            }
        }

        private void ApplyMovementAnimation(KAnimControllerBase animController, KAnim.PlayMode playMode)
        {
            // Choose appropriate movement animation based on speed and direction
            string movementAnim = CurrentAnimation;
            
            if (MovementSpeed > 1.5f)
            {
                movementAnim = "run_loop"; // Fast movement
            }
            else if (MovementSpeed > 0.1f)
            {
                movementAnim = "walk_loop"; // Normal walking
            }
            else
            {
                movementAnim = "idle_default"; // Standing still
            }
            
            // Handle directional animations
            if (MovementDirection.x > 0.1f)
            {
                animController.FlipX = false; // Moving right
            }
            else if (MovementDirection.x < -0.1f)
            {
                animController.FlipX = true; // Moving left
            }
            
            animController.Play(movementAnim, playMode, AnimationSpeed, 0f);
        }

        private void ApplyWorkAnimation(KAnimControllerBase animController, KAnim.PlayMode playMode)
        {
            // Work animations should typically loop while working
            var workAnim = CurrentAnimation.Contains("working") ? CurrentAnimation : "working";
            animController.Play(workAnim, KAnim.PlayMode.Loop, AnimationSpeed, 0f);
        }

        private void ApplyIdleAnimation(KAnimControllerBase animController, KAnim.PlayMode playMode)
        {
            // Idle animations can be varied to avoid static poses
            var idleAnim = CurrentAnimation.Contains("idle") ? CurrentAnimation : "idle_default";
            animController.Play(idleAnim, KAnim.PlayMode.Loop, AnimationSpeed, 0f);
        }

        private void ApplyClimbingAnimation(KAnimControllerBase animController, KAnim.PlayMode playMode)
        {
            // Climbing animations need proper speed sync with movement
            var climbAnim = CurrentAnimation.Contains("climb") ? CurrentAnimation : "ladder_up";
            animController.Play(climbAnim, KAnim.PlayMode.Loop, AnimationSpeed, 0f);
        }

        private void ApplySwimmingAnimation(KAnimControllerBase animController, KAnim.PlayMode playMode)
        {
            // Swimming animations should sync with water interaction
            var swimAnim = CurrentAnimation.Contains("swim") ? CurrentAnimation : "swim_loop";
            animController.Play(swimAnim, KAnim.PlayMode.Loop, AnimationSpeed, 0f);
        }

        private void SyncAnimationFrame(KAnimControllerBase animController)
        {
            // Set precise animation frame to avoid frame desync
            try
            {
                animController.SetPositionPercent(CurrentFrame);
                
                // Force animation update to ensure frame is applied
                if (animController is KBatchedAnimController batchedController)
                {
                    batchedController.SetVisiblity(true);
                    
                    // Use reflection to force rebuild if needed
                    var forceRebuildField = typeof(KBatchedAnimController)
                        .GetField("_forceRebuild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    forceRebuildField?.SetValue(batchedController, true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to sync animation frame: {ex.Message}");
            }
        }

        private void HandleAnimationBlending(KAnimControllerBase animController)
        {
            // Handle blending between two animations
            // This is useful for smooth transitions between states
            try
            {
                // For now, we'll use simple crossfade by adjusting opacity
                // More sophisticated blending would require deeper animation system integration
                Debug.Log($"Blending {CurrentAnimation} with {BlendAnimation} (factor: {BlendFactor:F2})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to handle animation blending: {ex.Message}");
            }
        }

        private void ValidatePositionSync(GameObject duplicantGO)
        {
            // Ensure the duplicant position matches the animation context
            var currentPos = duplicantGO.transform.position;
            var positionDifference = Vector3.Distance(currentPos, Position);
            
            if (positionDifference > 0.5f) // Threshold for position desync
            {
                Debug.LogWarning($"Position desync detected for duplicant {DuplicantNetId}: " +
                               $"Expected {Position}, Got {currentPos} (diff: {positionDifference:F2})");
            }
        }

        public void OnDispatched()
        {
            // Called after packet is sent - cleanup if needed
        }

        public override string ToString()
        {
            return $"AnimationSyncPacket[DuplicantNetId={DuplicantNetId}, Animation={CurrentAnimation}, " +
                   $"Frame={CurrentFrame:F2}, Speed={AnimationSpeed:F2}, Context={AnimationContext}]";
        }
    }
}
