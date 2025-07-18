using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Animation
{
    /// <summary>
    /// Animation loop control packet for managing continuous animations.
    /// Handles loop timing, cycle counts, and seamless loop transitions.
    /// Essential for walking, running, and other repetitive animations.
    /// </summary>
    public class AnimationLoopPacket : IPacket
    {
        public int DuplicantNetId;
        public string AnimationName;         // Name of the looping animation
        public bool IsLooping;               // Whether animation should loop
        public int LoopCount;                // Number of loops completed
        public float LoopDuration;           // Duration of one complete loop
        public float LoopProgress;           // Progress within current loop (0.0 to 1.0)
        public float LoopSpeed;              // Speed of loop playback
        public bool SeamlessLoop;            // Whether loop should be seamless
        public float LoopStartFrame;         // Frame where loop begins
        public float LoopEndFrame;           // Frame where loop ends
        public string NextAnimation;         // Animation to transition to after loop ends
        public int MaxLoops;                 // Maximum number of loops (-1 = infinite)
        public System.DateTime LoopStartTime;       // When current loop started

        public PacketType Type => PacketType.AnimationLoop;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(AnimationName ?? "");
            writer.Write(IsLooping);
            writer.Write(LoopCount);
            writer.Write(LoopDuration);
            writer.Write(LoopProgress);
            writer.Write(LoopSpeed);
            writer.Write(SeamlessLoop);
            writer.Write(LoopStartFrame);
            writer.Write(LoopEndFrame);
            writer.Write(NextAnimation ?? "");
            writer.Write(MaxLoops);
            writer.Write(System.DateTime.UtcNow.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            AnimationName = reader.ReadString();
            IsLooping = reader.ReadBoolean();
            LoopCount = reader.ReadInt32();
            LoopDuration = reader.ReadSingle();
            LoopProgress = reader.ReadSingle();
            LoopSpeed = reader.ReadSingle();
            SeamlessLoop = reader.ReadBoolean();
            LoopStartFrame = reader.ReadSingle();
            LoopEndFrame = reader.ReadSingle();
            NextAnimation = reader.ReadString();
            MaxLoops = reader.ReadInt32();
            LoopStartTime = System.DateTime.FromBinary(reader.ReadInt64());
        }

        public bool IsValid()
        {
            return DuplicantNetId > 0 && 
                   !string.IsNullOrEmpty(AnimationName) && 
                   LoopProgress >= 0f && 
                   LoopProgress <= 1f;
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantNetId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"AnimationLoopPacket: Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var animController = duplicantGO.GetComponent<KAnimControllerBase>();
            if (animController == null)
            {
                Debug.LogWarning($"AnimationLoopPacket: Duplicant {DuplicantNetId} has no animation controller");
                return;
            }

            try
            {
                ApplyLoopControl(animController);
                
                DebugConsole.Log($"[AnimationLoop] Applied loop control to {AnimationName} for duplicant {DuplicantNetId} " +
                               $"(Loop: {LoopCount}, Progress: {LoopProgress:F2}, Speed: {LoopSpeed:F2})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply animation loop control for duplicant {DuplicantNetId}: {ex.Message}");
            }
        }

        private void ApplyLoopControl(KAnimControllerBase animController)
        {
            if (IsLooping)
            {
                StartLoopAnimation(animController);
            }
            else
            {
                StopLoopAnimation(animController);
            }
        }

        private void StartLoopAnimation(KAnimControllerBase animController)
        {
            try
            {
                // Determine play mode based on loop settings
                var playMode = SeamlessLoop ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once;
                
                // Start the loop animation
                animController.Play(AnimationName, playMode, LoopSpeed, 0f);
                
                // Set the current loop progress
                SetLoopProgress(animController);
                
                // Handle loop-specific configurations
                ConfigureLoopBehavior(animController);
                
                // Set up loop timing
                SetupLoopTiming(animController);
                
                DebugConsole.Log($"Started loop animation {AnimationName} for duplicant {DuplicantNetId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to start loop animation: {ex.Message}");
            }
        }

        private void StopLoopAnimation(KAnimControllerBase animController)
        {
            try
            {
                // If we have a next animation, transition to it
                if (!string.IsNullOrEmpty(NextAnimation))
                {
                    animController.Play(NextAnimation, KAnim.PlayMode.Once, 1f, 0f);
                    DebugConsole.Log($"Transitioned from loop {AnimationName} to {NextAnimation} for duplicant {DuplicantNetId}");
                }
                else
                {
                    // Stop the current animation
                    animController.Stop();
                    DebugConsole.Log($"Stopped loop animation {AnimationName} for duplicant {DuplicantNetId}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to stop loop animation: {ex.Message}");
            }
        }

        private void SetLoopProgress(KAnimControllerBase animController)
        {
            try
            {
                // Calculate the actual frame position within the loop
                var loopFramePosition = Mathf.Lerp(LoopStartFrame, LoopEndFrame, LoopProgress);
                
                // Ensure frame position is within valid bounds
                loopFramePosition = Mathf.Clamp01(loopFramePosition);
                
                // Apply the frame position
                animController.SetPositionPercent(loopFramePosition);
                
                // For seamless loops, handle wrap-around
                if (SeamlessLoop && LoopProgress >= 1f)
                {
                    animController.SetPositionPercent(LoopStartFrame);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to set loop progress: {ex.Message}");
            }
        }

        private void ConfigureLoopBehavior(KAnimControllerBase animController)
        {
            try
            {
                // Configure loop-specific behavior based on animation type
                switch (AnimationName)
                {
                    case "walk_loop":
                    case "run_loop":
                        ConfigureMovementLoop(animController);
                        break;
                    case "idle_loop":
                    case "idle_default":
                        ConfigureIdleLoop(animController);
                        break;
                    case "working_loop":
                        ConfigureWorkLoop(animController);
                        break;
                    case "climb_loop":
                        ConfigureClimbLoop(animController);
                        break;
                    default:
                        ConfigureGenericLoop(animController);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to configure loop behavior: {ex.Message}");
            }
        }

        private void ConfigureMovementLoop(KAnimControllerBase animController)
        {
            // Movement loops should be seamless and responsive to speed changes
            if (animController is KBatchedAnimController batchedController)
            {
                // Ensure smooth movement animation
                batchedController.SetVisiblity(true);
                
                // Adjust animation speed based on movement speed
                var adjustedSpeed = LoopSpeed * CalculateMovementSpeedMultiplier();
                
                // Use reflection to set animation speed if possible
                SetAnimationSpeed(batchedController, adjustedSpeed);
            }
        }

        private void ConfigureIdleLoop(KAnimControllerBase animController)
        {
            // Idle loops can be slower and more varied
            var idleSpeed = LoopSpeed * 0.8f; // Slightly slower idle animation
            SetAnimationSpeed(animController, idleSpeed);
        }

        private void ConfigureWorkLoop(KAnimControllerBase animController)
        {
            // Work loops should match work rhythm
            var workSpeed = LoopSpeed * 1.2f; // Slightly faster work animation
            SetAnimationSpeed(animController, workSpeed);
        }

        private void ConfigureClimbLoop(KAnimControllerBase animController)
        {
            // Climbing loops should sync with movement speed
            var climbSpeed = LoopSpeed * CalculateClimbSpeedMultiplier();
            SetAnimationSpeed(animController, climbSpeed);
        }

        private void ConfigureGenericLoop(KAnimControllerBase animController)
        {
            // Generic loop configuration
            SetAnimationSpeed(animController, LoopSpeed);
        }

        private void SetupLoopTiming(KAnimControllerBase animController)
        {
            try
            {
                // Calculate expected loop completion time
                var expectedLoopTime = LoopDuration / LoopSpeed;
                var elapsedTime = (System.DateTime.UtcNow - LoopStartTime).TotalSeconds;
                
                // If we're significantly out of sync, adjust
                var expectedProgress = (float)(elapsedTime / expectedLoopTime) % 1f;
                var progressDifference = Mathf.Abs(expectedProgress - LoopProgress);
                
                if (progressDifference > 0.1f) // 10% tolerance
                {
                    Debug.LogWarning($"Loop timing sync issue for {AnimationName}: " +
                                   $"Expected {expectedProgress:F2}, got {LoopProgress:F2}");
                    
                    // Adjust timing to sync
                    animController.SetPositionPercent(expectedProgress);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to setup loop timing: {ex.Message}");
            }
        }

        private float CalculateMovementSpeedMultiplier()
        {
            // Calculate speed multiplier based on movement context
            // This would ideally get actual movement speed from duplicant
            return Mathf.Clamp(LoopSpeed, 0.5f, 2f);
        }

        private float CalculateClimbSpeedMultiplier()
        {
            // Calculate climb speed multiplier
            return Mathf.Clamp(LoopSpeed * 0.8f, 0.3f, 1.5f);
        }

        private void SetAnimationSpeed(KAnimControllerBase animController, float speed)
        {
            try
            {
                // Try to set animation speed through reflection
                var speedProperty = animController.GetType()
                    .GetProperty("PlaySpeed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (speedProperty != null && speedProperty.CanWrite)
                {
                    speedProperty.SetValue(animController, speed);
                }
                else
                {
                    // Fallback: restart animation with new speed
                    var playMode = SeamlessLoop ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once;
                    animController.Play(AnimationName, playMode, speed, 0f);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to set animation speed: {ex.Message}");
            }
        }

        public void OnDispatched()
        {
            // Called after packet is sent
        }

        public override string ToString()
        {
            return $"AnimationLoopPacket[DuplicantNetId={DuplicantNetId}, Animation={AnimationName}, " +
                   $"Loop={LoopCount}, Progress={LoopProgress:F2}, Speed={LoopSpeed:F2}]";
        }
    }
}
