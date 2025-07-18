using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Klei;
using LUKES_ONI_MP.Networking;
using LUKES_ONI_MP.Networking.Packets;

namespace LUKES_ONI_MP.Patches.DuplicantBehavior
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
        [HarmonyPatch(typeof(AmountInstance), nameof(AmountInstance.SetValue))]
        [HarmonyPostfix]
        public static void OnStressLevelChanged(AmountInstance __instance, float value)
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
                
                PacketSender.SendPacket(packet);
                
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
                
                var duplicant = __instance.gameObject?.GetComponent<MinionIdentity>();
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
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[StressBehaviorPatches] Synchronized stress reaction '{reactionType}' for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StressBehaviorPatches] Error in OnStressReactionTriggered: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes destructive stress behaviors like breaking buildings.
        /// </summary>
        [HarmonyPatch(typeof(BreakChore), ctor: new Type[] { typeof(IStateMachineTarget) })]
        [HarmonyPostfix]
        public static void OnBreakChoreStarted(BreakChore __instance, IStateMachineTarget target)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                var duplicant = target as GameObject;
                if (duplicant == null) return;
                
                var minionIdentity = duplicant.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant);
                if (duplicantId == -1) return;
                
                var packet = new DuplicantStressBehaviorPacket
                {
                    DuplicantId = duplicantId,
                    BehaviorType = "BreakBuilding",
                    TargetObjectId = -1, // Will be set when target is identified
                    IsActive = true,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[StressBehaviorPatches] Synchronized break chore start for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StressBehaviorPatches] Error in OnBreakChoreStarted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes ugly crying stress behavior.
        /// </summary>
        [HarmonyPatch(typeof(UglyCryChore), ctor: new Type[] { typeof(IStateMachineTarget) })]
        [HarmonyPostfix]
        public static void OnUglyCryChoreStarted(UglyCryChore __instance, IStateMachineTarget target)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                var duplicant = target as GameObject;
                if (duplicant == null) return;
                
                var minionIdentity = duplicant.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant);
                if (duplicantId == -1) return;
                
                var packet = new DuplicantStressBehaviorPacket
                {
                    DuplicantId = duplicantId,
                    BehaviorType = "UglyCry",
                    TargetObjectId = -1,
                    IsActive = true,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[StressBehaviorPatches] Synchronized ugly cry chore start for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StressBehaviorPatches] Error in OnUglyCryChoreStarted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes binge eating stress behavior.
        /// </summary>
        [HarmonyPatch(typeof(BingeEatChore), ctor: new Type[] { typeof(IStateMachineTarget) })]
        [HarmonyPostfix]
        public static void OnBingeEatChoreStarted(BingeEatChore __instance, IStateMachineTarget target)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                var duplicant = target as GameObject;
                if (duplicant == null) return;
                
                var minionIdentity = duplicant.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant);
                if (duplicantId == -1) return;
                
                var packet = new DuplicantStressBehaviorPacket
                {
                    DuplicantId = duplicantId,
                    BehaviorType = "BingeEat",
                    TargetObjectId = -1,
                    IsActive = true,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[StressBehaviorPatches] Synchronized binge eat chore start for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StressBehaviorPatches] Error in OnBingeEatChoreStarted: {ex.Message}");
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
                
                // Check if this is a stress-related chore
                bool isStressChore = __instance is BreakChore || 
                                   __instance is UglyCryChore || 
                                   __instance is BingeEatChore;
                
                if (!isStressChore) return;
                
                var driver = __instance.driver;
                if (driver?.gameObject == null) return;
                
                var minionIdentity = driver.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(driver.gameObject);
                if (duplicantId == -1) return;
                
                string behaviorType = __instance.GetType().Name.Replace("Chore", "");
                
                var packet = new DuplicantStressBehaviorPacket
                {
                    DuplicantId = duplicantId,
                    BehaviorType = behaviorType,
                    TargetObjectId = -1,
                    IsActive = false,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[StressBehaviorPatches] Synchronized stress chore end '{behaviorType}' for duplicant {duplicantId}");
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
                if (stressMonitor == null || !stressMonitor.IsStressed()) return;
                
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
                
                PacketSender.SendPacket(packet);
                
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
        [HarmonyPatch(typeof(MassageTable), "OnWorkCompleted")]
        [HarmonyPostfix]
        public static void OnStressReliefActivity(MassageTable __instance, Worker worker)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (worker?.gameObject == null) return;
                
                var minionIdentity = worker.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(worker.gameObject);
                int massageTableId = NetworkIdentityRegistry.GetNetworkId(__instance.gameObject);
                
                if (duplicantId == -1 || massageTableId == -1) return;
                
                var packet = new StressReliefActivityPacket
                {
                    DuplicantId = duplicantId,
                    ActivityType = "MassageTable",
                    BuildingId = massageTableId,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
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
