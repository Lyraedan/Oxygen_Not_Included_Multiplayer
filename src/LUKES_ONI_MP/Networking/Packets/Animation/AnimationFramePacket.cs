using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Animation
{
    /// <summary>
    /// Precise animation frame synchronization packet.
    /// Handles frame-by-frame updates to prevent animations from getting stuck.
    /// Sends specific frame data with timing information for smooth playback.
    /// </summary>
    public class AnimationFramePacket : IPacket
    {
        public int DuplicantNetId;
        public string AnimationName;         // Animation being updated
        public float FramePosition;          // Exact frame position (0.0 to 1.0)
        public float FrameTime;              // Time elapsed in current frame
        public float FrameDuration;          // Total duration of current frame
        public int CurrentFrameIndex;        // Index of current frame in sequence
        public int TotalFrames;              // Total frames in animation
        public float DeltaTime;              // Time since last frame update
        public bool ForceUpdate;             // Force frame update even if same frame
        public long Timestamp;               // High-precision timestamp for sync

        public PacketType Type => PacketType.AnimationFrame;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(AnimationName ?? "");
            writer.Write(FramePosition);
            writer.Write(FrameTime);
            writer.Write(FrameDuration);
            writer.Write(CurrentFrameIndex);
            writer.Write(TotalFrames);
            writer.Write(DeltaTime);
            writer.Write(ForceUpdate);
            writer.Write(DateTime.UtcNow.Ticks);
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            AnimationName = reader.ReadString();
            FramePosition = reader.ReadSingle();
            FrameTime = reader.ReadSingle();
            FrameDuration = reader.ReadSingle();
            CurrentFrameIndex = reader.ReadInt32();
            TotalFrames = reader.ReadInt32();
            DeltaTime = reader.ReadSingle();
            ForceUpdate = reader.ReadBoolean();
            Timestamp = reader.ReadInt64();
        }

        public bool IsValid()
        {
            return DuplicantNetId > 0 && 
                   !string.IsNullOrEmpty(AnimationName) && 
                   FramePosition >= 0f && 
                   FramePosition <= 1f;
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantNetId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"AnimationFramePacket: Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var animController = duplicantGO.GetComponent<KAnimControllerBase>();
            if (animController == null)
            {
                Debug.LogWarning($"AnimationFramePacket: Duplicant {DuplicantNetId} has no animation controller");
                return;
            }

            try
            {
                ApplyFrameUpdate(animController);
                
                DebugConsole.Log($"[AnimationFrame] Updated {AnimationName} for duplicant {DuplicantNetId} " +
                               $"to frame {CurrentFrameIndex}/{TotalFrames} (pos: {FramePosition:F3})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply animation frame update for duplicant {DuplicantNetId}: {ex.Message}");
            }
        }

        private void ApplyFrameUpdate(KAnimControllerBase animController)
        {
            // Check if we need to update (avoid redundant updates)
            if (!ForceUpdate && !ShouldUpdateFrame(animController))
            {
                return;
            }

            // Apply precise frame positioning
            SetPreciseFrame(animController);
            
            // Handle frame timing for smooth playback
            HandleFrameTiming(animController);
            
            // Force visual update to prevent stuck frames
            ForceVisualUpdate(animController);
        }

        private bool ShouldUpdateFrame(KAnimControllerBase animController)
        {
            // Check if animation is currently playing the expected animation
            var currentAnim = GetCurrentAnimationName(animController);
            if (currentAnim != AnimationName)
            {
                Debug.Log($"Animation mismatch: Expected {AnimationName}, got {currentAnim}");
                return true; // Different animation, needs update
            }

            // Check if frame position has changed significantly
            var currentFramePos = GetCurrentFramePosition(animController);
            var frameDifference = Mathf.Abs(currentFramePos - FramePosition);
            
            return frameDifference > 0.01f; // Update if frame difference is significant
        }

        private void SetPreciseFrame(KAnimControllerBase animController)
        {
            try
            {
                // Set the exact frame position
                animController.SetPositionPercent(FramePosition);
                
                // For batched controllers, ensure the frame is applied immediately
                if (animController is KBatchedAnimController batchedController)
                {
                    // Use reflection to access internal frame setting methods
                    var animType = typeof(KBatchedAnimController);
                    
                    // Try to set the current frame directly
                    var currentFrameField = animType.GetField("currentFrame", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (currentFrameField != null)
                    {
                        currentFrameField.SetValue(batchedController, CurrentFrameIndex);
                    }
                    
                    // Force the animation to update its internal state
                    var updateMethod = animType.GetMethod("UpdateFrame", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    updateMethod?.Invoke(batchedController, new object[] { });
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to set precise frame: {ex.Message}");
                // Fallback to basic position setting
                animController.SetPositionPercent(FramePosition);
            }
        }

        private void HandleFrameTiming(KAnimControllerBase animController)
        {
            // Calculate network latency compensation
            var networkLatency = CalculateNetworkLatency();
            var compensatedTime = FrameTime + networkLatency;
            
            // Adjust animation timing to account for network delay
            if (compensatedTime > FrameDuration)
            {
                // We're behind, need to advance to next frame
                var nextFramePos = CalculateNextFramePosition();
                animController.SetPositionPercent(nextFramePos);
            }
            else if (compensatedTime < 0)
            {
                // We're ahead, hold current frame
                animController.SetPositionPercent(FramePosition);
            }
        }

        private void ForceVisualUpdate(KAnimControllerBase animController)
        {
            try
            {
                // Force the visual representation to update
                if (animController is KBatchedAnimController batchedController)
                {
                    // Mark for rebuild to ensure visual update
                    var forceRebuildField = typeof(KBatchedAnimController)
                        .GetField("_forceRebuild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    forceRebuildField?.SetValue(batchedController, true);
                    
                    // Suspend and resume updates to force refresh
                    var suspendUpdatesMethod = typeof(KBatchedAnimController)
                        .GetMethod("SuspendUpdates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var resumeUpdatesMethod = typeof(KBatchedAnimController)
                        .GetMethod("ResumeUpdates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    suspendUpdatesMethod?.Invoke(batchedController, new object[] { false });
                    resumeUpdatesMethod?.Invoke(batchedController, null);
                    
                    // Ensure visibility is maintained
                    batchedController.SetVisiblity(true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to force visual update: {ex.Message}");
            }
        }

        private float CalculateNetworkLatency()
        {
            // Calculate latency based on timestamp difference
            var currentTime = DateTime.UtcNow.Ticks;
            var latencyTicks = currentTime - Timestamp;
            var latencySeconds = (float)latencyTicks / TimeSpan.TicksPerSecond;
            
            // Cap latency compensation at reasonable bounds
            return Mathf.Clamp(latencySeconds, 0f, 0.1f); // Max 100ms compensation
        }

        private float CalculateNextFramePosition()
        {
            // Calculate what the frame position should be based on timing
            var frameProgress = (CurrentFrameIndex + 1f) / TotalFrames;
            return Mathf.Clamp01(frameProgress);
        }

        private string GetCurrentAnimationName(KAnimControllerBase animController)
        {
            try
            {
                // Try to get current animation name through reflection
                var currentAnimField = animController.GetType()
                    .GetField("currentAnim", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (currentAnimField?.GetValue(animController) is KAnim.Anim currentAnim)
                {
                    return currentAnim.name;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to get current animation name: {ex.Message}");
            }
            
            return "unknown";
        }

        private float GetCurrentFramePosition(KAnimControllerBase animController)
        {
            try
            {
                // Try to get current frame position
                var positionMethod = animController.GetType()
                    .GetMethod("GetPositionPercent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (positionMethod != null)
                {
                    return (float)positionMethod.Invoke(animController, null);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to get current frame position: {ex.Message}");
            }
            
            return 0f;
        }

        public void OnDispatched()
        {
            // Called after packet is sent
        }

        public override string ToString()
        {
            return $"AnimationFramePacket[DuplicantNetId={DuplicantNetId}, Animation={AnimationName}, " +
                   $"Frame={CurrentFrameIndex}/{TotalFrames}, Position={FramePosition:F3}]";
        }
    }
}
