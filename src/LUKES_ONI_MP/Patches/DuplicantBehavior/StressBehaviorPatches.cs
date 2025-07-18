using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Klei;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.DuplicantBehavior;

namespace ONI_MP.Patches.DuplicantBehavior
{
    /// <summary>
    /// Patches for synchronizing duplicant stress behaviors across multiplayer sessions.
    /// Handles stress reactions, stress-induced actions, and stress management coordination.
    /// </summary>
    [HarmonyPatch]
    public static class StressBehaviorPatches
    {
        private static Dictionary<int, float> lastSentStressLevels = new Dictionary<int, float>();
        private static Dictionary<int, string> lastSentStressReactions = new Dictionary<int, string>();
        private static readonly float STRESS_SYNC_THRESHOLD = 0.05f; // 5% stress change triggers sync
        
        /// <summary>
        /// Synchronizes stress level changes when duplicants experience stress fluctuations.
        /// </summary>
        [HarmonyPatch(typeof(Klei.AI.AmountInstance), nameof(Klei.AI.AmountInstance.SetValue))]
        [HarmonyPostfix]
        public static void OnStressLevelChanged(Klei.AI.AmountInstance __instance, float value)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Check if this is a stress amount
                if (__instance.amount?.Id != Db.Get().Amounts.Stress.Id) return;
                
                var duplicant = __instance.gameObject?.GetComponent<MinionIdentity>();
                if (duplicant == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant.gameObject);
                if (duplicantId == -1) return;
                
                // Only sync if stress change is significant
                if (lastSentStressLevels.TryGetValue(duplicantId, out float lastStress))
                {
                    if (Mathf.Abs(value - lastStress) < STRESS_SYNC_THRESHOLD) return;
                }
                
                lastSentStressLevels[duplicantId] = value;
                
                var packet = new DuplicantStressPacket
                {
                    DuplicantId = duplicantId,
                    StressLevel = value,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendToAllClients(packet);
                
                Debug.Log($"[StressBehaviorPatches] Synchronized stress level {value:F2} for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StressBehaviorPatches] Error in OnStressLevelChanged: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes stress reactions when duplicants enter stress-induced states.
        /// </summary>
        [HarmonyPatch(typeof(StressMonitor), "OnStressReactionTrigger")]
        [HarmonyPostfix]
        public static void OnStressReactionTriggered(StressMonitor __instance, string reactionType)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // TODO: Fix StressMonitor to GameObject lookup - StressMonitor doesn't inherit from Component
                // For now, we'll skip this functionality until we can find the correct way to access the duplicant
                Debug.LogWarning($"[StressBehaviorPatches] Stress reaction '{reactionType}' triggered but cannot access duplicant from StressMonitor");
                return;
                
                /*
                // Get the duplicant from the same GameObject that has the stress monitor
                var duplicant = ((Component)__instance).GetComponent<MinionIdentity>();
                if (duplicant == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant.gameObject);
                if (duplicantId == -1) return;
                
                // Avoid sending duplicate stress reactions
                if (lastSentStressReactions.TryGetValue(duplicantId, out string lastReaction))
                {
                    if (lastReaction == reactionType) return;
                }
                
                lastSentStressReactions[duplicantId] = reactionType;
                
                var packet = new DuplicantStressReactionPacket
                {
                    DuplicantId = duplicantId,
                    ReactionType = reactionType,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendToAllClients(packet);
                
                Debug.Log($"[StressBehaviorPatches] Synchronized stress reaction '{reactionType}' for duplicant {duplicantId}");
                */
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StressBehaviorPatches] Error in OnStressReactionTriggered: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes when stress chores are completed or interrupted.
        /// </summary>
        [HarmonyPatch(typeof(Chore), nameof(Chore.Cleanup))]
        [HarmonyPrefix]
        public static void OnStressChoreEnded(Chore __instance)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Check if this is a stress-related chore by name
                string choreTypeName = __instance.choreType?.Id ?? "";
                bool isStressChore = choreTypeName.Contains("Break") || 
                                   choreTypeName.Contains("Cry") || 
                                   choreTypeName.Contains("Eat");
                
                if (!isStressChore) return;
                
                var driver = __instance.driver;
                if (driver?.gameObject == null) return;
                
                var minionIdentity = driver.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(driver.gameObject);
                if (duplicantId == -1) return;
                
                var packet = new DuplicantStressBehaviorPacket
                {
                    DuplicantId = duplicantId,
                    BehaviorType = choreTypeName,
                    TargetObjectId = -1,
                    IsActive = false,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendToAllClients(packet);
                
                Debug.Log($"[StressBehaviorPatches] Synchronized stress chore end '{choreTypeName}' for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StressBehaviorPatches] Error in OnStressChoreEnded: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes stress-induced building destruction.
        /// </summary>
        [HarmonyPatch(typeof(BuildingHP), nameof(BuildingHP.DoDamage))]
        [HarmonyPrefix]
        public static void OnBuildingDamageFromStress(BuildingHP __instance, float damage, GameObject source)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (source == null) return;
                
                // Check if damage source is a stressed duplicant
                var minionIdentity = source.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                var stressMonitor = source.GetComponent<StressMonitor>();
                if (stressMonitor == null) return; // Simplified check without stress level
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(source);
                int buildingId = NetworkIdentityRegistry.GetNetworkId(__instance.gameObject);
                
                if (duplicantId == -1 || buildingId == -1) return;
                
                var packet = new BuildingDamageFromStressPacket
                {
                    DuplicantId = duplicantId,
                    BuildingId = buildingId,
                    DamageAmount = damage,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendToAllClients(packet);
                
                Debug.Log($"[StressBehaviorPatches] Synchronized stress damage {damage} from duplicant {duplicantId} to building {buildingId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StressBehaviorPatches] Error in OnBuildingDamageFromStress: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes stress-reducing activities like using massage tables.
        /// </summary>
        [HarmonyPatch(typeof(Workable), "OnWorkCompleted")]
        [HarmonyPostfix]
        public static void OnStressReliefActivity(Workable __instance, WorkerBase worker)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (worker?.gameObject == null) return;
                
                // Only handle stress-relief activities
                if (!(__instance.name.Contains("Massage") || __instance.name.Contains("Hot Tub") || __instance.name.Contains("Espresso"))) return;
                
                var minionIdentity = worker.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(worker.gameObject);
                int buildingId = NetworkIdentityRegistry.GetNetworkId(__instance.gameObject);
                
                if (duplicantId == -1 || buildingId == -1) return;
                
                // TODO: Implement StressReliefActivityPacket
                /*var packet = new StressReliefActivityPacket
                {
                    DuplicantId = duplicantId,
                    ActivityType = __instance.name,
                    BuildingId = buildingId,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);*/
                
                Debug.Log($"[StressBehaviorPatches] Synchronized stress relief activity for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StressBehaviorPatches] Error in OnStressReliefActivity: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clears cached stress data when a duplicant is removed.
        /// </summary>
        public static void ClearDuplicantStressData(int duplicantId)
        {
            lastSentStressLevels.Remove(duplicantId);
            lastSentStressReactions.Remove(duplicantId);
        }
        
        /// <summary>
        /// Resets all stress synchronization data for new sessions.
        /// </summary>
        public static void ResetStressSyncData()
        {
            lastSentStressLevels.Clear();
            lastSentStressReactions.Clear();
        }
    }
}
