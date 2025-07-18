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
        [HarmonyPatch(typeof(IdleChore), ctor: new Type[] { typeof(IStateMachineTarget) })]
        [HarmonyPostfix]
        public static void OnIdleChoreStarted(IdleChore __instance, IStateMachineTarget target)
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
                
                // Track idle state
                currentIdleActivities[duplicantId] = "General";
                idleStateTimestamps[duplicantId] = Time.time;
                lastIdlePositions[duplicantId] = duplicant.transform.position;
                
                var packet = new DuplicantIdleStatePacket
                {
                    DuplicantId = duplicantId,
                    IdleType = "General",
                    IsIdle = true,
                    Position = duplicant.transform.position,
                    BuildingId = -1,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
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
        [HarmonyPatch(typeof(IdleChore), nameof(IdleChore.Cleanup))]
        [HarmonyPrefix]
        public static void OnIdleChoreEnded(IdleChore __instance)
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
                
                // Clear idle state
                currentIdleActivities.Remove(duplicantId);
                idleStateTimestamps.Remove(duplicantId);
                
                var packet = new DuplicantIdleStatePacket
                {
                    DuplicantId = duplicantId,
                    IdleType = "None",
                    IsIdle = false,
                    Position = driver.gameObject.transform.position,
                    BuildingId = -1,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
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
        [HarmonyPatch(typeof(RelaxationPoint), "OnWorkCompleted")]
        [HarmonyPostfix]
        public static void OnRecreationCompleted(RelaxationPoint __instance, Worker worker)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (worker?.gameObject == null) return;
                
                var minionIdentity = worker.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(worker.gameObject);
                int buildingId = NetworkIdentityRegistry.GetNetworkId(__instance.gameObject);
                
                if (duplicantId == -1 || buildingId == -1) return;
                
                // Update recreation tracking
                duplicantRecreationBuildings[duplicantId] = buildingId;
                
                var packet = new DuplicantRecreationPacket
                {
                    DuplicantId = duplicantId,
                    RecreationType = __instance.GetType().Name,
                    BuildingId = buildingId,
                    RecreationValue = GetRecreationValue(__instance),
                    Duration = GetRecreationDuration(__instance),
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[IdleBehaviorPatches] Recreation completed for duplicant {duplicantId} at building {buildingId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IdleBehaviorPatches] Error in OnRecreationCompleted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes social interactions between duplicants.
        /// </summary>
        [HarmonyPatch(typeof(SocialChore), ctor: new Type[] { typeof(IStateMachineTarget), typeof(ChoreType), typeof(IStateMachineTarget), typeof(bool), typeof(Action<Chore>), typeof(Action<Chore>), typeof(Action<Chore>) })]
        [HarmonyPostfix]
        public static void OnSocialChoreStarted(SocialChore __instance, IStateMachineTarget initiator, ChoreType chore_type, IStateMachineTarget recipient)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                var initiatorObject = initiator as GameObject;
                var recipientObject = recipient as GameObject;
                
                if (initiatorObject == null || recipientObject == null) return;
                
                var initiatorIdentity = initiatorObject.GetComponent<MinionIdentity>();
                var recipientIdentity = recipientObject.GetComponent<MinionIdentity>();
                
                if (initiatorIdentity == null || recipientIdentity == null) return;
                
                int initiatorId = NetworkIdentityRegistry.GetNetworkId(initiatorObject);
                int recipientId = NetworkIdentityRegistry.GetNetworkId(recipientObject);
                
                if (initiatorId == -1 || recipientId == -1) return;
                
                var packet = new DuplicantSocialInteractionPacket
                {
                    InitiatorId = initiatorId,
                    RecipientId = recipientId,
                    InteractionType = chore_type?.Id ?? "Unknown",
                    InteractionStartTime = System.DateTime.UtcNow.ToBinary(),
                    Position = initiatorObject.transform.position,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[IdleBehaviorPatches] Social interaction '{chore_type?.Id}' started between {initiatorId} and {recipientId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IdleBehaviorPatches] Error in OnSocialChoreStarted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes idle movement and wandering behavior.
        /// </summary>
        [HarmonyPatch(typeof(Navigator), nameof(Navigator.SetCurrentNavType))]
        [HarmonyPostfix]
        public static void OnNavigationStateChanged(Navigator __instance, NavType nav_type)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                
                // Only track idle movement
                if (nav_type != NavType.Floor) return;
                
                var duplicant = __instance.gameObject;
                if (duplicant == null) return;
                
                var minionIdentity = duplicant.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant);
                if (duplicantId == -1) return;
                
                // Check if duplicant is currently idle
                if (!currentIdleActivities.ContainsKey(duplicantId)) return;
                
                Vector3 currentPosition = duplicant.transform.position;
                
                // Only sync if position change is significant
                if (lastIdlePositions.TryGetValue(duplicantId, out Vector3 lastPosition))
                {
                    if (Vector3.Distance(currentPosition, lastPosition) < IDLE_POSITION_THRESHOLD) return;
                }
                
                lastIdlePositions[duplicantId] = currentPosition;
                
                var packet = new DuplicantIdleMovementPacket
                {
                    DuplicantId = duplicantId,
                    Position = currentPosition,
                    NavType = nav_type.ToString(),
                    MovementSpeed = __instance.defaultSpeed,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[IdleBehaviorPatches] Idle movement synchronized for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IdleBehaviorPatches] Error in OnNavigationStateChanged: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes duplicant reactions to entertainment buildings.
        /// </summary>
        [HarmonyPatch(typeof(JukeBot), "OnWorkCompleted")]
        [HarmonyPostfix]
        public static void OnJukeBotUsed(JukeBot __instance, Worker worker)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (worker?.gameObject == null) return;
                
                var minionIdentity = worker.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) return;
                
                int duplicantId = NetworkIdentityRegistry.GetNetworkId(worker.gameObject);
                int jukeBotId = NetworkIdentityRegistry.GetNetworkId(__instance.gameObject);
                
                if (duplicantId == -1 || jukeBotId == -1) return;
                
                var packet = new DuplicantEntertainmentPacket
                {
                    DuplicantId = duplicantId,
                    EntertainmentType = "JukeBot",
                    BuildingId = jukeBotId,
                    EntertainmentValue = 3.0f, // JukeBot provides entertainment
                    EffectRadius = 5.0f, // JukeBot affects nearby duplicants
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[IdleBehaviorPatches] JukeBot entertainment synchronized for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IdleBehaviorPatches] Error in OnJukeBotUsed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes duplicant reactions to decorative items during idle time.
        /// </summary>
        [HarmonyPatch(typeof(DecorProvider), "GetDecorForCell")]
        [HarmonyPostfix]
        public static void OnDecorObserved(DecorProvider __instance, int cell, float __result)
        {
            try
            {
                if (!MultiplayerSession.IsActive()) return;
                if (__result <= 0) return; // Only sync positive decor
                
                // Find duplicants near this decor cell
                var duplicants = GameObject.FindObjectsOfType<MinionIdentity>();
                foreach (var duplicant in duplicants)
                {
                    if (duplicant == null) continue;
                    
                    int duplicantId = NetworkIdentityRegistry.GetNetworkId(duplicant.gameObject);
                    if (duplicantId == -1) continue;
                    
                    // Check if duplicant is idle and near the decor
                    if (!currentIdleActivities.ContainsKey(duplicantId)) continue;
                    
                    Vector3 duplicantPos = duplicant.transform.position;
                    Vector3 cellPos = Grid.CellToPosCBC(cell, Grid.SceneLayer.Move);
                    
                    if (Vector3.Distance(duplicantPos, cellPos) > 3.0f) continue;
                    
                    int decorId = NetworkIdentityRegistry.GetNetworkId(__instance.gameObject);
                    if (decorId == -1) continue;
                    
                    var packet = new DuplicantDecorInteractionPacket
                    {
                        DuplicantId = duplicantId,
                        DecorObjectId = decorId,
                        DecorValue = __result,
                        InteractionType = "Observe",
                        Duration = 2.0f,
                        Timestamp = System.DateTime.UtcNow.ToBinary()
                    };
                    
                    PacketSender.SendPacket(packet);
                    
                    Debug.Log($"[IdleBehaviorPatches] Decor observation synchronized for duplicant {duplicantId}");
                    break; // Only sync one duplicant per decor observation
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IdleBehaviorPatches] Error in OnDecorObserved: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronizes idle eating and snacking behaviors.
        /// </summary>
        [HarmonyPatch(typeof(EatChore), ctor: new Type[] { typeof(IStateMachineTarget) })]
        [HarmonyPostfix]
        public static void OnIdleEatingStarted(EatChore __instance, IStateMachineTarget target)
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
                
                // Check if this is recreational eating (not work-based)
                var driver = __instance.driver;
                if (driver?.GetCurrentChore()?.choreType?.priority < 300) return; // Low priority = recreational
                
                var packet = new DuplicantIdleActivityPacket
                {
                    DuplicantId = duplicantId,
                    ActivityType = "RecreationalEating",
                    Duration = 10.0f, // Typical eating duration
                    Position = duplicant.transform.position,
                    Priority = __instance.choreType?.priority ?? 0,
                    Timestamp = System.DateTime.UtcNow.ToBinary()
                };
                
                PacketSender.SendPacket(packet);
                
                Debug.Log($"[IdleBehaviorPatches] Recreational eating synchronized for duplicant {duplicantId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IdleBehaviorPatches] Error in OnIdleEatingStarted: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets recreation value from a relaxation point.
        /// </summary>
        private static float GetRecreationValue(RelaxationPoint relaxationPoint)
        {
            try
            {
                // Different relaxation points provide different recreation values
                if (relaxationPoint is EspressoMachine) return 4.0f;
                if (relaxationPoint is JukeBot) return 3.0f;
                if (relaxationPoint is WaterCooler) return 2.0f;
                return 1.0f; // Default recreation value
            }
            catch
            {
                return 1.0f;
            }
        }
        
        /// <summary>
        /// Gets recreation duration from a relaxation point.
        /// </summary>
        private static float GetRecreationDuration(RelaxationPoint relaxationPoint)
        {
            try
            {
                // Different activities have different durations
                if (relaxationPoint is EspressoMachine) return 15.0f;
                if (relaxationPoint is JukeBot) return 20.0f;
                if (relaxationPoint is WaterCooler) return 10.0f;
                return 12.0f; // Default duration
            }
            catch
            {
                return 12.0f;
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
