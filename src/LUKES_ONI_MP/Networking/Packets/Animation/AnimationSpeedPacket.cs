using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Animation
{
    /// <summary>
    /// Animation speed control packet for dynamic speed adjustments.
    /// Handles speed changes based on movement, work efficiency, and game state.
    /// Essential for synchronized movement speed and animation timing.
    /// </summary>
    public class AnimationSpeedPacket : IPacket
    {
        public int DuplicantNetId;
        public string AnimationName;         // Animation being speed-adjusted
        public float BaseSpeed;              // Base animation speed (1.0 = normal)
        public float SpeedMultiplier;        // Speed multiplier for current state
        public float TargetSpeed;            // Target speed for smooth transitions
        public float SpeedTransitionTime;    // Time to transition to target speed
        public SpeedContext Context;         // Context for speed change
        public float MovementVelocity;       // Current movement velocity
        public float WorkEfficiency;         // Work efficiency affecting speed
        public bool IsInstantaneous;         // Whether speed change is immediate
        public float MinSpeed;               // Minimum allowed speed
        public float MaxSpeed;               // Maximum allowed speed
        public System.DateTime SpeedChangeTime;     // When speed change was initiated

        public PacketType Type => PacketType.AnimationSpeed;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(AnimationName ?? "");
            writer.Write(BaseSpeed);
            writer.Write(SpeedMultiplier);
            writer.Write(TargetSpeed);
            writer.Write(SpeedTransitionTime);
            writer.Write((int)Context);
            writer.Write(MovementVelocity);
            writer.Write(WorkEfficiency);
            writer.Write(IsInstantaneous);
            writer.Write(MinSpeed);
            writer.Write(MaxSpeed);
            writer.Write(System.DateTime.UtcNow.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            AnimationName = reader.ReadString();
            BaseSpeed = reader.ReadSingle();
            SpeedMultiplier = reader.ReadSingle();
            TargetSpeed = reader.ReadSingle();
            SpeedTransitionTime = reader.ReadSingle();
            Context = (SpeedContext)reader.ReadInt32();
            MovementVelocity = reader.ReadSingle();
            WorkEfficiency = reader.ReadSingle();
            IsInstantaneous = reader.ReadBoolean();
            MinSpeed = reader.ReadSingle();
            MaxSpeed = reader.ReadSingle();
            SpeedChangeTime = System.DateTime.FromBinary(reader.ReadInt64());
        }

        public bool IsValid()
        {
            return DuplicantNetId > 0 && 
                   !string.IsNullOrEmpty(AnimationName) && 
                   TargetSpeed > 0f;
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantNetId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"AnimationSpeedPacket: Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var animController = duplicantGO.GetComponent<KAnimControllerBase>();
            if (animController == null)
            {
                Debug.LogWarning($"AnimationSpeedPacket: Duplicant {DuplicantNetId} has no animation controller");
                return;
            }

            try
            {
                ApplySpeedChange(animController);
                
                DebugConsole.Log($"[AnimationSpeed] Applied speed {TargetSpeed:F2} to {AnimationName} " +
                               $"for duplicant {DuplicantNetId} (Context: {Context})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply animation speed change for duplicant {DuplicantNetId}: {ex.Message}");
            }
        }

        private void ApplySpeedChange(KAnimControllerBase animController)
        {
            if (IsInstantaneous)
            {
                ApplyInstantaneousSpeed(animController);
            }
            else
            {
                ApplyGradualSpeedChange(animController);
            }
        }

        private void ApplyInstantaneousSpeed(KAnimControllerBase animController)
        {
            try
            {
                var finalSpeed = CalculateFinalSpeed();
                SetAnimationSpeed(animController, finalSpeed);
                
                DebugConsole.Log($"Instant speed change to {finalSpeed:F2} for {AnimationName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed instantaneous speed change: {ex.Message}");
            }
        }

        private void ApplyGradualSpeedChange(KAnimControllerBase animController)
        {
            try
            {
                // Calculate current progress in speed transition
                var elapsedTime = (System.DateTime.UtcNow - SpeedChangeTime).TotalSeconds;
                var progress = Mathf.Clamp01((float)(elapsedTime / SpeedTransitionTime));
                
                // Interpolate between current speed and target speed
                var currentSpeed = GetCurrentAnimationSpeed(animController);
                var interpolatedSpeed = Mathf.Lerp(currentSpeed, TargetSpeed, progress);
                var finalSpeed = CalculateFinalSpeed(interpolatedSpeed);
                
                SetAnimationSpeed(animController, finalSpeed);
                
                if (progress >= 1f)
                {
                    DebugConsole.Log($"Speed transition completed: {finalSpeed:F2} for {AnimationName}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed gradual speed change: {ex.Message}");
            }
        }

        private float CalculateFinalSpeed(float? overrideSpeed = null)
        {
            var workingSpeed = overrideSpeed ?? TargetSpeed;
            
            // Apply context-specific speed modifications
            workingSpeed = ApplyContextModifications(workingSpeed);
            
            // Apply multipliers
            workingSpeed *= SpeedMultiplier;
            
            // Apply movement velocity influence
            workingSpeed = ApplyMovementInfluence(workingSpeed);
            
            // Apply work efficiency influence
            workingSpeed = ApplyWorkEfficiencyInfluence(workingSpeed);
            
            // Clamp to min/max bounds
            workingSpeed = Mathf.Clamp(workingSpeed, MinSpeed, MaxSpeed);
            
            return workingSpeed;
        }

        private float ApplyContextModifications(float speed)
        {
            switch (Context)
            {
                case SpeedContext.Movement:
                    return ApplyMovementSpeedContext(speed);
                case SpeedContext.Work:
                    return ApplyWorkSpeedContext(speed);
                case SpeedContext.Idle:
                    return ApplyIdleSpeedContext(speed);
                case SpeedContext.Emergency:
                    return ApplyEmergencySpeedContext(speed);
                case SpeedContext.Tired:
                    return ApplyTiredSpeedContext(speed);
                case SpeedContext.Efficient:
                    return ApplyEfficientSpeedContext(speed);
                default:
                    return speed;
            }
        }

        private float ApplyMovementSpeedContext(float speed)
        {
            // Movement context: speed should match actual movement velocity
            if (MovementVelocity > 0f)
            {
                // Scale animation speed based on movement velocity
                var velocityMultiplier = Mathf.Clamp(MovementVelocity / 2f, 0.5f, 2f);
                return speed * velocityMultiplier;
            }
            return speed;
        }

        private float ApplyWorkSpeedContext(float speed)
        {
            // Work context: slower, more deliberate animations
            return speed * 0.85f; // Slightly slower for work animations
        }

        private float ApplyIdleSpeedContext(float speed)
        {
            // Idle context: relaxed, slower animations
            return speed * 0.7f;
        }

        private float ApplyEmergencySpeedContext(float speed)
        {
            // Emergency context: faster, more urgent animations
            return speed * 1.5f;
        }

        private float ApplyTiredSpeedContext(float speed)
        {
            // Tired context: significantly slower animations
            return speed * 0.6f;
        }

        private float ApplyEfficientSpeedContext(float speed)
        {
            // Efficient context: optimally fast animations
            return speed * 1.2f;
        }

        private float ApplyMovementInfluence(float speed)
        {
            // Adjust speed based on movement velocity for movement-related animations
            if (AnimationName.Contains("walk") || AnimationName.Contains("run") || AnimationName.Contains("move"))
            {
                if (MovementVelocity > 0f)
                {
                    // Faster movement = faster animation
                    var movementInfluence = Mathf.Clamp(MovementVelocity / 3f, 0.3f, 2f);
                    return speed * movementInfluence;
                }
            }
            return speed;
        }

        private float ApplyWorkEfficiencyInfluence(float speed)
        {
            // Adjust speed based on work efficiency for work-related animations
            if (AnimationName.Contains("work") || AnimationName.Contains("dig") || AnimationName.Contains("build"))
            {
                if (WorkEfficiency > 0f)
                {
                    // Higher efficiency = faster work animations
                    var efficiencyMultiplier = Mathf.Clamp(WorkEfficiency, 0.5f, 1.5f);
                    return speed * efficiencyMultiplier;
                }
            }
            return speed;
        }

        private void SetAnimationSpeed(KAnimControllerBase animController, float speed)
        {
            try
            {
                // Method 1: Try to set play speed property
                if (TrySetPlaySpeed(animController, speed))
                {
                    return;
                }
                
                // Method 2: Try to restart animation with new speed
                if (TryRestartWithSpeed(animController, speed))
                {
                    return;
                }
                
                // Method 3: Use reflection to set internal speed
                TrySetSpeedViaReflection(animController, speed);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to set animation speed: {ex.Message}");
            }
        }

        private bool TrySetPlaySpeed(KAnimControllerBase animController, float speed)
        {
            try
            {
                var speedProperty = animController.GetType()
                    .GetProperty("PlaySpeed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (speedProperty != null && speedProperty.CanWrite)
                {
                    speedProperty.SetValue(animController, speed);
                    return true;
                }
            }
            catch (System.Exception)
            {
                // Ignore reflection errors
            }
            return false;
        }

        private bool TryRestartWithSpeed(KAnimControllerBase animController, float speed)
        {
            try
            {
                // Get current animation state
                var currentFrame = GetCurrentFramePosition(animController);
                var playMode = IsLoopingAnimation() ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once;
                
                // Restart animation with new speed
                animController.Play(AnimationName, playMode, speed, 0f);
                
                // Restore frame position
                animController.SetPositionPercent(currentFrame);
                return true;
            }
            catch (System.Exception)
            {
                // Ignore errors
            }
            return false;
        }

        private void TrySetSpeedViaReflection(KAnimControllerBase animController, float speed)
        {
            try
            {
                // Try various internal speed fields
                var animType = animController.GetType();
                
                var speedField = animType.GetField("playSpeed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                speedField?.SetValue(animController, speed);
                
                var rateField = animType.GetField("animSpeed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                rateField?.SetValue(animController, speed);
            }
            catch (System.Exception)
            {
                // Ignore reflection errors
            }
        }

        private float GetCurrentAnimationSpeed(KAnimControllerBase animController)
        {
            try
            {
                var speedProperty = animController.GetType()
                    .GetProperty("PlaySpeed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (speedProperty != null && speedProperty.CanRead)
                {
                    return (float)speedProperty.GetValue(animController);
                }
            }
            catch (System.Exception)
            {
                // Ignore errors
            }
            return 1f; // Default speed
        }

        private float GetCurrentFramePosition(KAnimControllerBase animController)
        {
            try
            {
                var positionMethod = animController.GetType()
                    .GetMethod("GetPositionPercent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (positionMethod != null)
                {
                    return (float)positionMethod.Invoke(animController, null);
                }
            }
            catch (System.Exception)
            {
                // Ignore errors
            }
            return 0f;
        }

        private bool IsLoopingAnimation()
        {
            return AnimationName.Contains("loop") || 
                   AnimationName.Contains("walk") || 
                   AnimationName.Contains("run") || 
                   AnimationName.Contains("idle") || 
                   AnimationName.Contains("work");
        }

        public void OnDispatched()
        {
            // Called after packet is sent
        }

        public override string ToString()
        {
            return $"AnimationSpeedPacket[DuplicantNetId={DuplicantNetId}, Animation={AnimationName}, " +
                   $"Speed={TargetSpeed:F2}, Context={Context}]";
        }
    }

    /// <summary>
    /// Contexts for animation speed changes
    /// </summary>
    public enum SpeedContext
    {
        Movement,     // Movement-related speed changes
        Work,         // Work-related speed changes
        Idle,         // Idle state speed changes
        Emergency,    // Emergency/urgent speed changes
        Tired,        // Tired/low energy speed changes
        Efficient     // High efficiency speed changes
    }
}
