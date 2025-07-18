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
    /// Patches for synchronizing duplicant sleep behaviors across multiplayer sessions.
    /// Handles sleep schedules, bed assignments, sleep quality, and sleep-related activities.
    /// </summary>
    [HarmonyPatch]
    public static class SleepBehaviorPatches
    {
        private static Dictionary<int, float> lastSentStaminaLevels = new Dictionary<int, float>();
        private static Dictionary<int, int> duplicantBedAssignments = new Dictionary<int, int>();
        private static Dictionary<int, bool> duplicantSleepStates = new Dictionary<int, bool>();
        private static readonly float STAMINA_SYNC_THRESHOLD = 0.08f; // 8% stamina change triggers sync
        
        /// <summary>
        /// Synchronizes stamina level changes when duplicants get tired or recover energy.
        /// </summary>
        [HarmonyPatch(typeof(Klei.AI.AmountInstance), nameof(Klei.AI.AmountInstance.SetValue))]
        [HarmonyPostfix]
        public static void OnStaminaLevelChanged(Klei.AI.AmountInstance __instance, float value)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Check if this is a stamina amount
                if (__instance.amount?.Id != Db.Get().Amounts.Stamina.Id) return;
                
                var duplicant = __instance.gameObject?.GetComponent<MinionIdentity>();
                if (duplicant == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant.gameObject);
                if (duplicantId == -1) return;
                
                // Only sync if stamina change is significant
                if (lastSentStaminaLevels.TryGetValue(duplicantId, out float lastStamina))
                {
                    if (Mathf.Abs(value - lastStamina) < STAMINA_SYNC_THRESHOLD) return;
                }
                
                lastSentStaminaLevels[duplicantId] = value;
                
                var packet = new DuplicantStaminaPacket
                {
                    DuplicantId = duplicantId,
                    StaminaLevel = value,
                    MaxStamina = __instance.GetMax(),
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendToAllClients(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Synchronized stamina level {value:F2} for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnStaminaLevelChanged: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes when duplicants start sleeping in beds.
        /// Note: Uses StandardChoreBase.Begin instead of abstract Chore.Begin which cannot be patched.
        /// </summary>
        [HarmonyPatch(typeof(StandardChoreBase), nameof(StandardChoreBase.Begin))]
        [HarmonyPostfix]
        public static void OnSleepChoreStarted(Chore __instance)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Check if this is a sleep chore
                if (!(__instance.choreType?.Id?.Contains("Sleep") == true)) return;
                
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
                
                // Try to find associated bed
                int bedId = -1;
                var beds = GameObject.FindObjectsOfType<Bed>();
                foreach (var bed in beds)
                {
                    if (Vector3.Distance(bed.transform.position, duplicant.transform.position) < 3.0f)
                    {
                        bedId = NetworkIdentityRegistry.GetNetworkId(bed.gameObject);
                        break;
                    }
                }
                
                // Track sleep state
                duplicantSleepStates[duplicantId] = true;
                if (bedId != -1)
                {
                    duplicantBedAssignments[duplicantId] = bedId;
                }
                
                // TODO: Implement DuplicantSleepStatePacket
                /*var packet = new DuplicantSleepStatePacket
                {
                    DuplicantId = duplicantId,
                    IsSleeping = true,
                    BedId = bedId,
                    BedIsOwned = bedId != -1,
                    IsInterruptable = true,
                    SleepStartTime = System.DateTime.UtcNow.ToBinary(),
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);*/
                
                Debug.Log($"[SleepBehaviorPatches] Duplicant {duplicantId} started sleeping in bed {bedId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnSleepChoreStarted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes when duplicants wake up or stop sleeping.
        /// NOTE: Chore.Cleanup is abstract and cannot be patched - temporarily disabled
        /// TODO: Find alternative method to detect chore completion
        /// </summary>
        /*
        [HarmonyPatch(typeof(Chore), nameof(Chore.Cleanup))]
        [HarmonyPrefix]
        public static void OnSleepChoreEnded(Chore __instance)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Check if this is a sleep chore
                if (!(__instance.choreType?.Id?.Contains("Sleep") == true)) return;
                
                var driver = __instance.driver;
                if (driver?.gameObject == null) return;
                
                var minionIdentity = driver.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(driver.gameObject);
                if (duplicantId == -1) return;
                
                // Update sleep state
                duplicantSleepStates[duplicantId] = false;
                
                // Get bed ID if available
                int bedId = duplicantBedAssignments.ContainsKey(duplicantId) ? duplicantBedAssignments[duplicantId] : -1;
                
                // TODO: Implement DuplicantSleepStatePacket
                // var packet = new DuplicantSleepStatePacket
                // {
                //     DuplicantId = duplicantId,
                //     IsSleeping = false,
                //     BedId = bedId,
                //     BedIsOwned = false,
                //     IsInterruptable = false,
                //     SleepStartTime = -1,
                //     Timestamp = System.DateTime.UtcNow.ToBinary()
                // };
                // 
                // PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Duplicant {duplicantId} stopped sleeping");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnSleepChoreEnded: {ex.Message}");
            }
        }
        */
        
        /// <summary>
        /// Synchronizes sleep quality effects and bonuses.
        /// NOTE: OnWorkCompleted method may not exist in current game version - temporarily disabled
        /// </summary>
        /*
        [HarmonyPatch(typeof(Workable), "OnWorkCompleted")]
        [HarmonyPostfix]
        public static void OnSleepQualityBonus(Workable __instance, WorkerBase worker)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (worker?.gameObject == null) return;
                
                // Only handle sleep-related buildings
                if (!(__instance.name.Contains("Sleep") || __instance.name.Contains("Bed") || __instance.name.Contains("Cot"))) return;
                
                var minionIdentity = worker.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(worker.gameObject);
                int buildingId = NetworkIdentityRegistry.GetNetworkId(__instance.gameObject);
                
                if (duplicantId == -1 || buildingId == -1) return;
                
                // TODO: Implement SleepQualityPacket
                //var packet = new SleepQualityPacket
                //{
                //    DuplicantId = duplicantId,
                //    QualityType = __instance.name,
                //    BuildingId = buildingId,
                //    QualityBonus = 1.0f,
                //    Timestamp = System.DateTime.UtcNow.ToBinary()
                //};
                
                //PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Sleep quality bonus applied to duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnSleepQualityBonus: {ex.Message}");
            }
        }
        */
        
        /// <summary>
        /// Resets all sleep synchronization data for new sessions.
        /// </summary>
        public static void ResetSleepSyncData()
        {
            lastSentStaminaLevels.Clear();
            duplicantBedAssignments.Clear();
            duplicantSleepStates.Clear();
        }
    }
}
