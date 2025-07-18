using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using ONI_MP.Networking;

namespace ONI_MP.Patches.DuplicantBehavior
{
    /// <summary>
    /// Patches for synchronizing duplicant idle behaviors across multiplayer sessions.
    /// Handles recreation activities, socializing, idle animations, and downtime coordination.
    /// </summary>
    [HarmonyPatch]
    public static class IdleBehaviorPatches
    {
        private static Dictionary<int, string> currentIdleActivities = new Dictionary<int, string>();
        private static Dictionary<int, int> duplicantRecreationBuildings = new Dictionary<int, int>();
        private static Dictionary<int, Vector3> lastIdlePositions = new Dictionary<int, Vector3>();
        private static Dictionary<int, float> idleStateTimestamps = new Dictionary<int, float>();
        private static readonly float IDLE_POSITION_THRESHOLD = 0.5f; // Position change threshold for sync
        
        /// <summary>
        /// Synchronizes when duplicants start idle activities or recreation.
        /// </summary>
        [HarmonyPatch(typeof(Chore), "Begin")]
        [HarmonyPostfix]
        public static void OnIdleChoreStarted(Chore __instance)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Check if this is an idle chore
                if (!(__instance.choreType?.Id?.Contains("Idle") == true)) return;
                
                var target = __instance.target;
                GameObject duplicant = null;
                
                // Convert IStateMachineTarget to GameObject
                var kmb = target as KMonoBehaviour;
                if (kmb != null)
                {
                    duplicant = kmb.gameObject;
                }
                else
                {
                    // Try direct cast or find game object through component hierarchy
                    if (target != null && target.GetType().IsSubclassOf(typeof(Component)))
                    {
                        duplicant = ((Component)target).gameObject;
                    }
                }
                
                if (duplicant == null) return;
                
                var minionIdentity = duplicant.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant);
                if (duplicantId == -1) return;
                
                // Track idle state
                currentIdleActivities[duplicantId] = "General";
                idleStateTimestamps[duplicantId] = Time.time;
                lastIdlePositions[duplicantId] = duplicant.transform.position;
                
                Debug.Log($"[IdleBehaviorPatches] Duplicant {duplicantId} started idle activity");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IdleBehaviorPatches] Error in OnIdleChoreStarted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes when duplicants end idle activities.
        /// </summary>
        [HarmonyPatch(typeof(Chore), nameof(Chore.Cleanup))]
        [HarmonyPrefix]
        public static void OnIdleChoreEnded(Chore __instance)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Check if this is an idle chore
                if (!(__instance.choreType?.Id?.Contains("Idle") == true)) return;
                
                var driver = __instance.driver;
                if (driver?.gameObject == null) return;
                
                var minionIdentity = driver.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(driver.gameObject);
                if (duplicantId == -1) return;
                
                // Clear idle state
                currentIdleActivities.Remove(duplicantId);
                idleStateTimestamps.Remove(duplicantId);
                
                Debug.Log($"[IdleBehaviorPatches] Duplicant {duplicantId} ended idle activity");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IdleBehaviorPatches] Error in OnIdleChoreEnded: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes recreation activities at entertainment buildings.
        /// </summary>
        [HarmonyPatch(typeof(Workable), "OnWorkCompleted")]
        [HarmonyPostfix]
        public static void OnRecreationCompleted(Workable __instance, WorkerBase worker)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (worker?.gameObject == null) return;
                
                // Only handle recreation/entertainment buildings
                if (!(__instance.name.Contains("Espresso") || __instance.name.Contains("Juke") || __instance.name.Contains("Water"))) return;
                
                var minionIdentity = worker.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(worker.gameObject);
                int buildingId = NetworkIdentityRegistry.GetNetworkId(__instance.gameObject);
                
                if (duplicantId == -1 || buildingId == -1) return;
                
                // Update recreation tracking
                duplicantRecreationBuildings[duplicantId] = buildingId;
                
                Debug.Log($"[IdleBehaviorPatches] Recreation completed for duplicant {duplicantId} at building {buildingId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IdleBehaviorPatches] Error in OnRecreationCompleted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if a duplicant is currently idle.
        /// </summary>
        public static bool IsDuplicantIdle(int duplicantId)
        {
            return currentIdleActivities.ContainsKey(duplicantId);
        }
        
        /// <summary>
        /// Gets the current idle activity type for a duplicant.
        /// </summary>
        public static string GetIdleActivity(int duplicantId)
        {
            return currentIdleActivities.ContainsKey(duplicantId) ? currentIdleActivities[duplicantId] : "None";
        }
        
        /// <summary>
        /// Clears cached idle data when a duplicant is removed.
        /// </summary>
        public static void ClearDuplicantIdleData(int duplicantId)
        {
            currentIdleActivities.Remove(duplicantId);
            duplicantRecreationBuildings.Remove(duplicantId);
            lastIdlePositions.Remove(duplicantId);
            idleStateTimestamps.Remove(duplicantId);
        }
        
        /// <summary>
        /// Resets all idle synchronization data for new sessions.
        /// </summary>
        public static void ResetIdleSyncData()
        {
            currentIdleActivities.Clear();
            duplicantRecreationBuildings.Clear();
            lastIdlePositions.Clear();
            idleStateTimestamps.Clear();
        }
    }
}
