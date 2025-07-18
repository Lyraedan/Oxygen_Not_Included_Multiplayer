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
        [HarmonyPatch(typeof(AmountInstance), nameof(AmountInstance.SetValue))]
        [HarmonyPostfix]
        public static void OnStaminaLevelChanged(AmountInstance __instance, float value)
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
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Synchronized stamina level {value:F2} for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnStaminaLevelChanged: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes when duplicants start sleeping in beds.
        /// </summary>
        [HarmonyPatch(typeof(SleepChore), ctor: new Type[] { typeof(IStateMachineTarget), typeof(Bed), typeof(bool), typeof(bool) })]
        [HarmonyPostfix]
        public static void OnSleepChoreStarted(SleepChore __instance, IStateMachineTarget target, Bed bed, bool bedIsOwned, bool isInterruptable)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                var duplicant = target as GameObject;
                if (duplicant == null) return;
                
                var minionIdentity = duplicant.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant);
                int bedId = bed != null ? NetworkIdentityRegistry.GetNetworkId(bed.gameObject) : -1;
                
                if (duplicantId == -1) return;
                
                // Track sleep state
                duplicantSleepStates[duplicantId] = true;
                if (bedId != -1)
                {
                    duplicantBedAssignments[duplicantId] = bedId;
                }
                
                var packet = new DuplicantSleepStatePacket
                {
                    DuplicantId = duplicantId,
                    IsSleeping = true,
                    BedId = bedId,
                    BedIsOwned = bedIsOwned,
                    IsInterruptable = isInterruptable,
                    SleepStartTime = System.DateTime.UtcNow.ToBinary(),
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Duplicant {duplicantId} started sleeping in bed {bedId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnSleepChoreStarted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes when duplicants wake up or stop sleeping.
        /// </summary>
        [HarmonyPatch(typeof(SleepChore), nameof(SleepChore.Cleanup))]
        [HarmonyPrefix]
        public static void OnSleepChoreEnded(SleepChore __instance)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
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
                
                var packet = new DuplicantSleepStatePacket
                {
                    DuplicantId = duplicantId,
                    IsSleeping = false,
                    BedId = bedId,
                    BedIsOwned = false,
                    IsInterruptable = false,
                    SleepStartTime = -1,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Duplicant {duplicantId} stopped sleeping");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnSleepChoreEnded: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes bed ownership assignments when duplicants claim beds.
        /// </summary>
        [HarmonyPatch(typeof(Assignables), nameof(Assignables.Assign))]
        [HarmonyPostfix]
        public static void OnBedAssigned(Assignables __instance, AssignableSlotInstance slot, IAssignableIdentity assignee)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Check if this is a bed assignment
                var bed = slot?.assignable?.GetComponent<Bed>();
                if (bed == null) return;
                
                var minionIdentity = assignee as MinionIdentity;
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(minionIdentity.gameObject);
                int bedId = NetworkIdentityRegistry.GetNetworkId(bed.gameObject);
                
                if (duplicantId == -1 || bedId == -1) return;
                
                // Update assignment tracking
                duplicantBedAssignments[duplicantId] = bedId;
                
                var packet = new BedAssignmentPacket
                {
                    DuplicantId = duplicantId,
                    BedId = bedId,
                    IsAssigned = true,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Bed {bedId} assigned to duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnBedAssigned: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes bed unassignments when duplicants lose bed ownership.
        /// </summary>
        [HarmonyPatch(typeof(Assignables), nameof(Assignables.Unassign))]
        [HarmonyPostfix]
        public static void OnBedUnassigned(Assignables __instance, AssignableSlotInstance slot)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Check if this is a bed unassignment
                var bed = slot?.assignable?.GetComponent<Bed>();
                if (bed == null) return;
                
                int bedId = NetworkIdentityRegistry.GetNetworkId(bed.gameObject);
                if (bedId == -1) return;
                
                // Find which duplicant was unassigned
                int duplicantId = -1;
                foreach (var kvp in duplicantBedAssignments)
                {
                    if (kvp.Value == bedId)
                    {
                        duplicantId = kvp.Key;
                        break;
                    }
                }
                
                if (duplicantId != -1)
                {
                    duplicantBedAssignments.Remove(duplicantId);
                    
                    var packet = new BedAssignmentPacket
                    {
                        DuplicantId = duplicantId,
                        BedId = bedId,
                        IsAssigned = false,
                        Timestamp = System.DateTime.UtcNow.ToBinary()
                    };
                    
                    PacketSender.SendPacket(packet);
                    
                    Debug.Log($"[SleepBehaviorPatches] Bed {bedId} unassigned from duplicant {duplicantId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnBedUnassigned: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes sleep quality effects and bonuses.
        /// </summary>
        [HarmonyPatch(typeof(SleepClinic), "OnWorkCompleted")]
        [HarmonyPostfix]
        public static void OnSleepQualityBonus(SleepClinic __instance, Worker worker)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (worker?.gameObject == null) return;
                
                var minionIdentity = worker.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(worker.gameObject);
                int sleepClinicId = NetworkIdentityRegistry.GetNetworkId(__instance.gameObject);
                
                if (duplicantId == -1 || sleepClinicId == -1) return;
                
                var packet = new SleepQualityPacket
                {
                    DuplicantId = duplicantId,
                    QualityType = "SleepClinic",
                    BuildingId = sleepClinicId,
                    QualityBonus = 1.0f, // Sleep clinic provides quality bonus
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Sleep quality bonus applied to duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnSleepQualityBonus: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes sleep schedule changes from schedule management.
        /// </summary>
        [HarmonyPatch(typeof(ScheduleManager), nameof(ScheduleManager.SetSchedule))]
        [HarmonyPostfix]
        public static void OnScheduleChanged(ScheduleManager __instance, Ref<MinionIdentity> minion, ScheduleGroup scheduleGroup)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (minion?.Get() == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(minion.Get().gameObject);
                if (duplicantId == -1) return;
                
                // Extract sleep schedule information
                var sleepBlocks = new List<int>();
                for (int i = 0; i < scheduleGroup.blocks.Count; i++)
                {
                    if (scheduleGroup.blocks[i].GroupId == Db.Get().ScheduleBlockTypes.Sleep.Id)
                    {
                        sleepBlocks.Add(i);
                    }
                }
                
                var packet = new DuplicantSleepSchedulePacket
                {
                    DuplicantId = duplicantId,
                    ScheduleName = scheduleGroup.Name,
                    SleepTimeBlocks = sleepBlocks.ToArray(),
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Sleep schedule updated for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnScheduleChanged: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes narcoleptic sleep episodes for duplicants with that trait.
        /// </summary>
        [HarmonyPatch(typeof(NarcolepsyMonitor), "TriggerNarcolepsyReaction")]
        [HarmonyPostfix]
        public static void OnNarcolepsyTriggered(NarcolepsyMonitor __instance)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                var duplicant = __instance.gameObject;
                if (duplicant == null) return;
                
                var minionIdentity = duplicant.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant);
                if (duplicantId == -1) return;
                
                var packet = new DuplicantSleepEventPacket
                {
                    DuplicantId = duplicantId,
                    EventType = "Narcolepsy",
                    Duration = 10.0f, // Typical narcolepsy episode duration
                    Location = duplicant.transform.position,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Narcolepsy episode triggered for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnNarcolepsyTriggered: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes sleep interruptions due to noise, light, or other factors.
        /// </summary>
        [HarmonyPatch(typeof(SleepChore.StatesInstance), "IsInterrupted")]
        [HarmonyPostfix]
        public static void OnSleepInterrupted(SleepChore.StatesInstance __instance, bool __result)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (!__result) return; // Only sync if actually interrupted
                
                var duplicant = __instance.sm?.gameObject;
                if (duplicant == null) return;
                
                var minionIdentity = duplicant.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant);
                if (duplicantId == -1) return;
                
                var packet = new DuplicantSleepEventPacket
                {
                    DuplicantId = duplicantId,
                    EventType = "SleepInterrupted",
                    Duration = 0.0f,
                    Location = duplicant.transform.position,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[SleepBehaviorPatches] Sleep interrupted for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SleepBehaviorPatches] Error in OnSleepInterrupted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if a duplicant is currently sleeping.
        /// </summary>
        public static bool IsDuplicantSleeping(int duplicantId)
        {
            return duplicantSleepStates.ContainsKey(duplicantId) && duplicantSleepStates[duplicantId];
        }
        
        /// <summary>
        /// Gets the bed assignment for a duplicant.
        /// </summary>
        public static int GetDuplicantBedAssignment(int duplicantId)
        {
            return duplicantBedAssignments.ContainsKey(duplicantId) ? duplicantBedAssignments[duplicantId] : -1;
        }
        
        /// <summary>
        /// Clears cached sleep data when a duplicant is removed.
        /// </summary>
        public static void ClearDuplicantSleepData(int duplicantId)
        {
            lastSentStaminaLevels.Remove(duplicantId);
            duplicantBedAssignments.Remove(duplicantId);
            duplicantSleepStates.Remove(duplicantId);
        }
        
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
