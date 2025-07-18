using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Klei.AI;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.DuplicantBehavior;
using UnityEngine;

namespace ONI_MP.Misc.DuplicantBehavior
{
    /// <summary>
    /// Manages duplicant behavior synchronization across all multiplayer clients.
    /// Coordinates work assignments, pathfinding, idle behaviors, sleep cycles, and stress responses.
    /// </summary>
    public static class DuplicantBehaviorManager
    {
        // Update intervals for different behavior types (in seconds)
        private const float WORK_ASSIGNMENT_INTERVAL = 2.0f;    // Work assignments
        private const float IDLE_BEHAVIOR_INTERVAL = 5.0f;      // Idle behaviors and recreation
        private const float SLEEP_BEHAVIOR_INTERVAL = 3.0f;     // Sleep cycles and rest
        private const float STRESS_BEHAVIOR_INTERVAL = 1.5f;    // Stress responses (more frequent)
        private const float PATHFINDING_INTERVAL = 1.0f;        // Enhanced pathfinding
        private const float BEHAVIOR_STATE_INTERVAL = 2.5f;     // General behavior states

        // Packet queues for batched sending
        private static readonly Queue<WorkAssignmentPacket> workAssignmentQueue = new Queue<WorkAssignmentPacket>();
        private static readonly Queue<IdleBehaviorPacket> idleBehaviorQueue = new Queue<IdleBehaviorPacket>();
        private static readonly Queue<SleepBehaviorPacket> sleepBehaviorQueue = new Queue<SleepBehaviorPacket>();
        private static readonly Queue<StressBehaviorPacket> stressBehaviorQueue = new Queue<StressBehaviorPacket>();
        private static readonly Queue<PathfindingUpdatePacket> pathfindingQueue = new Queue<PathfindingUpdatePacket>();
        private static readonly Queue<BehaviorStatePacket> behaviorStateQueue = new Queue<BehaviorStatePacket>();

        // Last known states for change detection
        private static readonly Dictionary<int, string> lastWorkAssignments = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> lastIdleBehaviors = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> lastSleepStates = new Dictionary<int, string>();
        private static readonly Dictionary<int, float> lastStressLevels = new Dictionary<int, float>();
        private static readonly Dictionary<int, Vector3> lastPathingDestinations = new Dictionary<int, Vector3>();
        private static readonly Dictionary<int, string> lastBehaviorStates = new Dictionary<int, string>();

        // Timers for different update cycles
        private static float workAssignmentTimer;
        private static float idleBehaviorTimer;
        private static float sleepBehaviorTimer;
        private static float stressBehaviorTimer;
        private static float pathfindingTimer;
        private static float behaviorStateTimer;

        private static bool isInitialized = false;

        /// <summary>
        /// Initialize the duplicant behavior manager and start update coroutines.
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;

            DebugConsole.Log("[DuplicantBehaviorManager] Initializing duplicant behavior synchronization");

            // Start update coroutines using Game.Instance
            if (Game.Instance != null)
            {
                Game.Instance.StartCoroutine(WorkAssignmentUpdateLoop());
                Game.Instance.StartCoroutine(IdleBehaviorUpdateLoop());
                Game.Instance.StartCoroutine(SleepBehaviorUpdateLoop());
                Game.Instance.StartCoroutine(StressBehaviorUpdateLoop());
                Game.Instance.StartCoroutine(PathfindingUpdateLoop());
                Game.Instance.StartCoroutine(BehaviorStateUpdateLoop());
            }
            else
            {
                DebugConsole.LogWarning("[DuplicantBehaviorManager] Game.Instance is null, cannot start coroutines");
                return;
            }

            isInitialized = true;
            DebugConsole.Log("[DuplicantBehaviorManager] Initialization complete");
        }

        /// <summary>
        /// Shutdown the manager and clear all state.
        /// </summary>
        public static void Shutdown()
        {
            if (!isInitialized) return;

            DebugConsole.Log("[DuplicantBehaviorManager] Shutting down");

            // Clear all queues and state
            lock (workAssignmentQueue) workAssignmentQueue.Clear();
            lock (idleBehaviorQueue) idleBehaviorQueue.Clear();
            lock (sleepBehaviorQueue) sleepBehaviorQueue.Clear();
            lock (stressBehaviorQueue) stressBehaviorQueue.Clear();
            lock (pathfindingQueue) pathfindingQueue.Clear();
            lock (behaviorStateQueue) behaviorStateQueue.Clear();

            lastWorkAssignments.Clear();
            lastIdleBehaviors.Clear();
            lastSleepStates.Clear();
            lastStressLevels.Clear();
            lastPathingDestinations.Clear();
            lastBehaviorStates.Clear();

            isInitialized = false;
        }

        // Work Assignment Management
        public static void QueueWorkAssignment(int duplicantNetId, string choreTypeId, Vector3 targetPos, 
                                              int targetCell, string prefabId = "", float priority = 5.0f, 
                                              string role = "", bool isUrgent = false)
        {
            if (!MultiplayerSession.IsHost) return;

            lock (workAssignmentQueue)
            {
                var packet = new WorkAssignmentPacket
                {
                    DuplicantNetId = duplicantNetId,
                    ChoreTypeId = choreTypeId,
                    TargetPosition = targetPos,
                    TargetCell = targetCell,
                    TargetPrefabId = prefabId,
                    Priority = priority,
                    WorkerRole = role,
                    IsUrgent = isUrgent,
                    TimeEstimate = EstimateWorkTime(choreTypeId),
                    RequiredSkills = GetRequiredSkills(choreTypeId),
                    AssignmentReason = "Host Assignment"
                };

                workAssignmentQueue.Enqueue(packet);
            }
        }

        // Idle Behavior Management
        public static void QueueIdleBehavior(int duplicantNetId, string activity, Vector3 location, 
                                           string recreationType = "", float stressLevel = 0f, 
                                           bool isScheduled = false)
        {
            if (!MultiplayerSession.IsHost) return;

            lock (idleBehaviorQueue)
            {
                var packet = new IdleBehaviorPacket
                {
                    DuplicantNetId = duplicantNetId,
                    IdleActivity = activity,
                    RecreationType = recreationType,
                    IdlePosition = location,
                    RecreationTargetCell = Grid.PosToCell(location),
                    StressLevel = stressLevel,
                    IsScheduledBreak = isScheduled,
                    IdleReason = isScheduled ? "ScheduledDowntime" : "NoWork"
                };

                idleBehaviorQueue.Enqueue(packet);
            }
        }

        // Sleep Behavior Management
        public static void QueueSleepBehavior(int duplicantNetId, string sleepState, Vector3 bedPos, 
                                             int bedNetId = -1, float tiredness = 0f, float quality = 1f)
        {
            if (!MultiplayerSession.IsHost) return;

            lock (sleepBehaviorQueue)
            {
                var packet = new SleepBehaviorPacket
                {
                    DuplicantNetId = duplicantNetId,
                    SleepState = sleepState,
                    BedPosition = bedPos,
                    BedNetId = bedNetId,
                    TirednessLevel = tiredness,
                    SleepQuality = quality,
                    IsScheduledSleep = true,
                    SleepStartTime = GameClock.Instance?.GetTime() ?? 0f
                };

                sleepBehaviorQueue.Enqueue(packet);
            }
        }

        // Stress Behavior Management
        public static void QueueStressBehavior(int duplicantNetId, string stressState, float stressLevel, 
                                              Vector3 location, string reaction = "", float morale = 0f)
        {
            if (!MultiplayerSession.IsHost) return;

            lock (stressBehaviorQueue)
            {
                var packet = new StressBehaviorPacket
                {
                    DuplicantNetId = duplicantNetId,
                    StressState = stressState,
                    StressLevel = stressLevel,
                    StressLocation = location,
                    StressReaction = reaction,
                    MoraleLevel = morale,
                    IsStressBreakdown = stressLevel > 90f,
                    StressCauses = DetectStressCauses(duplicantNetId)
                };

                stressBehaviorQueue.Enqueue(packet);
            }
        }

        // Pathfinding Management
        public static void QueuePathfindingUpdate(int duplicantNetId, string state, Vector3 destination, 
                                                 bool isEmergency = false, string blockingReason = "")
        {
            if (!MultiplayerSession.IsHost) return;

            lock (pathfindingQueue)
            {
                var packet = new PathfindingUpdatePacket
                {
                    DuplicantNetId = duplicantNetId,
                    PathfindingState = state,
                    CurrentDestination = destination,
                    DestinationCell = Grid.PosToCell(destination),
                    IsEmergencyNavigation = isEmergency,
                    BlockingReason = blockingReason,
                    PathingPriority = isEmergency ? 10f : 5f
                };

                pathfindingQueue.Enqueue(packet);
            }
        }

        // General Behavior State Management
        public static void QueueBehaviorState(int duplicantNetId, string behavior, string subtype, 
                                             Vector3 location, float progress = 0f, float efficiency = 1f)
        {
            if (!MultiplayerSession.IsHost) return;

            lock (behaviorStateQueue)
            {
                var packet = new BehaviorStatePacket
                {
                    DuplicantNetId = duplicantNetId,
                    CurrentBehavior = behavior,
                    BehaviorSubtype = subtype,
                    BehaviorLocation = location,
                    BehaviorProgress = progress,
                    EfficiencyModifier = efficiency,
                    IsInterruptible = behavior != "Breathing" && behavior != "Emergency"
                };

                behaviorStateQueue.Enqueue(packet);
            }
        }

        // Update loops for different behavior types
        private static IEnumerator WorkAssignmentUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(WORK_ASSIGNMENT_INTERVAL);
                
                if (MultiplayerSession.IsHost && MultiplayerSession.InSession)
                {
                    CheckAndSyncWorkAssignments();
                    ProcessWorkAssignmentQueue();
                }
            }
        }

        private static IEnumerator IdleBehaviorUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(IDLE_BEHAVIOR_INTERVAL);
                
                if (MultiplayerSession.IsHost && MultiplayerSession.InSession)
                {
                    CheckAndSyncIdleBehaviors();
                    ProcessIdleBehaviorQueue();
                }
            }
        }

        private static IEnumerator SleepBehaviorUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(SLEEP_BEHAVIOR_INTERVAL);
                
                if (MultiplayerSession.IsHost && MultiplayerSession.InSession)
                {
                    CheckAndSyncSleepBehaviors();
                    ProcessSleepBehaviorQueue();
                }
            }
        }

        private static IEnumerator StressBehaviorUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(STRESS_BEHAVIOR_INTERVAL);
                
                if (MultiplayerSession.IsHost && MultiplayerSession.InSession)
                {
                    CheckAndSyncStressBehaviors();
                    ProcessStressBehaviorQueue();
                }
            }
        }

        private static IEnumerator PathfindingUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(PATHFINDING_INTERVAL);
                
                if (MultiplayerSession.IsHost && MultiplayerSession.InSession)
                {
                    CheckAndSyncPathfinding();
                    ProcessPathfindingQueue();
                }
            }
        }

        private static IEnumerator BehaviorStateUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(BEHAVIOR_STATE_INTERVAL);
                
                if (MultiplayerSession.IsHost && MultiplayerSession.InSession)
                {
                    CheckAndSyncBehaviorStates();
                    ProcessBehaviorStateQueue();
                }
            }
        }

        // Check and sync methods that detect changes in duplicant behaviors
        private static void CheckAndSyncWorkAssignments()
        {
            var duplicants = UnityEngine.Object.FindObjectsOfType<ChoreDriver>();
            
            foreach (var driver in duplicants)
            {
                var identity = driver.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                var netId = identity.NetId;
                var currentChore = driver.GetCurrentChore();
                var currentChoreId = currentChore?.choreType?.Id ?? "None";

                if (!lastWorkAssignments.ContainsKey(netId) || lastWorkAssignments[netId] != currentChoreId)
                {
                    lastWorkAssignments[netId] = currentChoreId;
                    
                    if (currentChore != null)
                    {
                        QueueWorkAssignment(netId, currentChoreId, 
                            currentChore.gameObject?.transform.position ?? Vector3.zero,
                            Grid.PosToCell(currentChore.gameObject),
                            currentChore.gameObject?.PrefabID().Name ?? "");
                    }
                }
            }
        }

        private static void CheckAndSyncIdleBehaviors()
        {
            var duplicants = UnityEngine.Object.FindObjectsOfType<MinionIdentity>();
            
            foreach (var minion in duplicants)
            {
                var identity = minion.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                var netId = identity.NetId;
                var choreDriver = minion.GetComponent<ChoreDriver>();
                var currentChore = choreDriver?.GetCurrentChore();

                string idleActivity = "None";
                if (currentChore == null || currentChore.choreType.Id == "IdleChore")
                {
                    idleActivity = "Idle";
                }
                else if (currentChore.choreType.Id == "Mingle" || currentChore.choreType.Id == "Party")
                {
                    idleActivity = "Recreation";
                }

                if (!lastIdleBehaviors.ContainsKey(netId) || lastIdleBehaviors[netId] != idleActivity)
                {
                    lastIdleBehaviors[netId] = idleActivity;
                    
                    if (idleActivity != "None")
                    {
                        QueueIdleBehavior(netId, idleActivity, minion.transform.position);
                    }
                }
            }
        }

        private static void CheckAndSyncSleepBehaviors()
        {
            var duplicants = UnityEngine.Object.FindObjectsOfType<MinionIdentity>();
            
            foreach (var minionIdentity in duplicants)
            {
                var identity = minionIdentity.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                var netId = identity.NetId;
                var choreDriver = minionIdentity.GetComponent<ChoreDriver>();
                var currentChore = choreDriver?.GetCurrentChore();

                // Get stamina component for stamina-related checks (simplified)
                var staminaComponent = minionIdentity.GetComponent<StaminaMonitor>();
                var staminaValue = 100f; // Default value when stamina access isn't available
                var maxStamina = 100f;   // Default max

                string sleepState = "Awake";
                if (currentChore?.choreType.Id == "Sleep")
                {
                    sleepState = "Sleeping";
                }
                else if (staminaValue < 20f)
                {
                    sleepState = "Tired";
                }

                if (!lastSleepStates.ContainsKey(netId) || lastSleepStates[netId] != sleepState)
                {
                    lastSleepStates[netId] = sleepState;
                    
                    var tirednessLevel = Mathf.Clamp01(1.0f - (staminaValue / maxStamina)) * 100f;
                    QueueSleepBehavior(netId, sleepState, minionIdentity.transform.position, -1, tirednessLevel);
                }
            }
        }

        private static void CheckAndSyncStressBehaviors()
        {
            var duplicants = UnityEngine.Object.FindObjectsOfType<MinionIdentity>();
            
            foreach (var minion in duplicants)
            {
                var identity = minion.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                var netId = identity.NetId;
                var attributes = minion.GetComponent<Klei.AI.Attributes>();
                
                if (attributes != null)
                {
                    var stressAttribute = attributes.Get(Db.Get().Attributes.QualityOfLife.Id);
                    var currentStress = stressAttribute != null ? 100f - stressAttribute.GetTotalValue() : 0f;

                    if (!lastStressLevels.ContainsKey(netId) || 
                        Mathf.Abs(lastStressLevels[netId] - currentStress) > 5f)
                    {
                        lastStressLevels[netId] = currentStress;
                        
                        string stressState = "Calm";
                        if (currentStress > 90f) stressState = "Breakdown";
                        else if (currentStress > 60f) stressState = "StressedOut";
                        else if (currentStress > 30f) stressState = "Stressed";

                        QueueStressBehavior(netId, stressState, currentStress, minion.transform.position);
                    }
                }
            }
        }

        private static void CheckAndSyncPathfinding()
        {
            var duplicants = UnityEngine.Object.FindObjectsOfType<Navigator>();
            
            foreach (var navigator in duplicants)
            {
                var identity = navigator.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                var netId = identity.NetId;
                var currentDestination = navigator.target?.transform.position ?? Vector3.zero;

                if (!lastPathingDestinations.ContainsKey(netId) || 
                    Vector3.Distance(lastPathingDestinations[netId], currentDestination) > 1f)
                {
                    lastPathingDestinations[netId] = currentDestination;
                    
                    string pathState = "PathFound";
                    if (!navigator.IsMoving()) pathState = "DestinationReached";
                    else if (!navigator.path.IsValid()) pathState = "PathCalculating";

                    QueuePathfindingUpdate(netId, pathState, currentDestination);
                }
            }
        }

        private static void CheckAndSyncBehaviorStates()
        {
            var duplicants = UnityEngine.Object.FindObjectsOfType<ChoreDriver>();
            
            foreach (var driver in duplicants)
            {
                var identity = driver.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                var netId = identity.NetId;
                var currentChore = driver.GetCurrentChore();
                var behaviorState = DetermineBehaviorState(currentChore);

                if (!lastBehaviorStates.ContainsKey(netId) || lastBehaviorStates[netId] != behaviorState)
                {
                    lastBehaviorStates[netId] = behaviorState;
                    
                    var (behavior, subtype) = ParseBehaviorState(behaviorState);
                    QueueBehaviorState(netId, behavior, subtype, driver.transform.position);
                }
            }
        }

        // Helper methods
        private static void ProcessWorkAssignmentQueue()
        {
            lock (workAssignmentQueue)
            {
                while (workAssignmentQueue.Count > 0)
                {
                    var packet = workAssignmentQueue.Dequeue();
                    PacketSender.SendToAllClients(packet);
                }
            }
        }

        private static void ProcessIdleBehaviorQueue()
        {
            lock (idleBehaviorQueue)
            {
                while (idleBehaviorQueue.Count > 0)
                {
                    var packet = idleBehaviorQueue.Dequeue();
                    PacketSender.SendToAllClients(packet);
                }
            }
        }

        private static void ProcessSleepBehaviorQueue()
        {
            lock (sleepBehaviorQueue)
            {
                while (sleepBehaviorQueue.Count > 0)
                {
                    var packet = sleepBehaviorQueue.Dequeue();
                    PacketSender.SendToAllClients(packet);
                }
            }
        }

        private static void ProcessStressBehaviorQueue()
        {
            lock (stressBehaviorQueue)
            {
                while (stressBehaviorQueue.Count > 0)
                {
                    var packet = stressBehaviorQueue.Dequeue();
                    PacketSender.SendToAllClients(packet);
                }
            }
        }

        private static void ProcessPathfindingQueue()
        {
            lock (pathfindingQueue)
            {
                while (pathfindingQueue.Count > 0)
                {
                    var packet = pathfindingQueue.Dequeue();
                    PacketSender.SendToAllClients(packet);
                }
            }
        }

        private static void ProcessBehaviorStateQueue()
        {
            lock (behaviorStateQueue)
            {
                while (behaviorStateQueue.Count > 0)
                {
                    var packet = behaviorStateQueue.Dequeue();
                    PacketSender.SendToAllClients(packet);
                }
            }
        }

        private static float EstimateWorkTime(string choreTypeId)
        {
            // Simple work time estimates based on chore type
            return choreTypeId switch
            {
                "Dig" => 2.0f,
                "Build" => 5.0f,
                "Repair" => 3.0f,
                "Harvest" => 1.5f,
                "Cook" => 4.0f,
                _ => 3.0f
            };
        }

        private static List<string> GetRequiredSkills(string choreTypeId)
        {
            // Map chore types to required skills
            return choreTypeId switch
            {
                "Dig" => new List<string> { "Mining" },
                "Build" => new List<string> { "Construction" },
                "Cook" => new List<string> { "Cooking" },
                "Research" => new List<string> { "Research" },
                _ => new List<string>()
            };
        }

        private static List<string> DetectStressCauses(int duplicantNetId)
        {
            var causes = new List<string>();
            
            if (NetworkIdentityRegistry.TryGet(duplicantNetId, out var entity))
            {
                var effects = entity.GetComponent<Effects>();
                if (effects != null)
                {
                    if (effects.HasEffect("Hot")) causes.Add("Heat");
                    if (effects.HasEffect("Cold")) causes.Add("Cold");
                    if (effects.HasEffect("BadFood")) causes.Add("BadFood");
                    if (effects.HasEffect("NoisePollution")) causes.Add("Noise");
                    if (effects.HasEffect("Alone")) causes.Add("Isolation");
                }
            }
            
            return causes;
        }

        private static string DetermineBehaviorState(Chore chore)
        {
            if (chore == null) return "Idle";
            
            return chore.choreType.Id switch
            {
                "Eat" => "Eating",
                "Sleep" => "Sleeping",
                "Dig" or "Build" or "Repair" => "Working",
                "Mingle" or "Party" => "Socializing",
                "IdleChore" => "Relaxing",
                _ => "Working"
            };
        }

        private static (string behavior, string subtype) ParseBehaviorState(string state)
        {
            return state switch
            {
                "Eating" => ("Eating", "Normal"),
                "Sleeping" => ("Sleeping", "Normal"),
                "Working" => ("Working", "General"),
                "Socializing" => ("Socializing", "Casual"),
                "Relaxing" => ("Relaxing", "Idle"),
                _ => ("Moving", "Walking")
            };
        }
    }
}
