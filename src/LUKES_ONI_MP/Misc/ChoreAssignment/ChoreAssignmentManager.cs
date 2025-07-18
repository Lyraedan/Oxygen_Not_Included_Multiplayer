using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets;
using System;
using UnityEngine;

namespace ONI_MP.Misc.ChoreAssignment
{
    /// <summary>
    /// Utility class for managing chore assignments across multiplayer sessions.
    /// Provides convenient methods for assigning chores to duplicants with different priority levels.
    /// </summary>
    public static class ChoreAssignmentManager
    {
        /// <summary>
        /// Assigns a chore to a specific duplicant by NetworkIdentity ID.
        /// </summary>
        /// <param name="duplicantNetId">Network ID of the target duplicant</param>
        /// <param name="choreTypeId">ID of the chore type to assign</param>
        /// <param name="targetPosition">Position where the chore should be performed</param>
        /// <param name="targetCell">Grid cell for the chore (optional, -1 if not applicable)</param>
        /// <param name="prefabId">Prefab ID of the target object (optional)</param>
        /// <param name="priority">Priority level (1-9, default 5)</param>
        /// <param name="isImmediate">Whether to immediately override current chore</param>
        /// <param name="isUrgent">Whether this is an urgent assignment</param>
        public static void AssignChore(int duplicantNetId, string choreTypeId, Vector3 targetPosition, 
                                      int targetCell = -1, string prefabId = "", float priority = 5.0f, 
                                      bool isImmediate = false, bool isUrgent = false)
        {
            if (!MultiplayerSession.InSession)
            {
                DebugConsole.LogWarning("[ChoreAssignmentManager] Cannot assign chore: not in multiplayer session");
                return;
            }

            if (!MultiplayerSession.IsHost)
            {
                DebugConsole.LogWarning("[ChoreAssignmentManager] Only host can assign chores");
                return;
            }

            var packet = new ChoreAssignmentPacket
            {
                NetId = duplicantNetId,
                ChoreTypeId = choreTypeId,
                TargetPosition = targetPosition,
                TargetCell = targetCell,
                TargetPrefabId = prefabId ?? "",
                Priority = Mathf.Clamp(priority, 1f, 9f),
                IsImmediate = isImmediate,
                IsUrgent = isUrgent
            };

            PacketSender.SendToAll(packet);
            
            DebugConsole.Log($"[ChoreAssignmentManager] Assigned {choreTypeId} to duplicant {duplicantNetId} " +
                           $"(Priority: {priority}, Urgent: {isUrgent}, Immediate: {isImmediate})");
        }

        /// <summary>
        /// Assigns an urgent chore that should immediately interrupt current activities.
        /// </summary>
        public static void AssignUrgentChore(int duplicantNetId, string choreTypeId, Vector3 targetPosition, 
                                           int targetCell = -1, string prefabId = "")
        {
            AssignChore(duplicantNetId, choreTypeId, targetPosition, targetCell, prefabId, 
                       priority: 9.0f, isImmediate: true, isUrgent: true);
        }

        /// <summary>
        /// Assigns a high-priority chore without interrupting current activities.
        /// </summary>
        public static void AssignHighPriorityChore(int duplicantNetId, string choreTypeId, Vector3 targetPosition, 
                                                  int targetCell = -1, string prefabId = "")
        {
            AssignChore(duplicantNetId, choreTypeId, targetPosition, targetCell, prefabId, 
                       priority: 8.0f, isImmediate: false, isUrgent: true);
        }

        /// <summary>
        /// Assigns a normal priority chore.
        /// </summary>
        public static void AssignNormalChore(int duplicantNetId, string choreTypeId, Vector3 targetPosition, 
                                           int targetCell = -1, string prefabId = "")
        {
            AssignChore(duplicantNetId, choreTypeId, targetPosition, targetCell, prefabId, 
                       priority: 5.0f, isImmediate: false, isUrgent: false);
        }

        /// <summary>
        /// Assigns a low-priority chore for background tasks.
        /// </summary>
        public static void AssignBackgroundChore(int duplicantNetId, string choreTypeId, Vector3 targetPosition, 
                                                int targetCell = -1, string prefabId = "")
        {
            AssignChore(duplicantNetId, choreTypeId, targetPosition, targetCell, prefabId, 
                       priority: 2.0f, isImmediate: false, isUrgent: false);
        }

        /// <summary>
        /// Assigns a chore to the nearest available duplicant within a radius.
        /// </summary>
        /// <param name="origin">Starting position for search</param>
        /// <param name="radius">Search radius</param>
        /// <param name="choreTypeId">Chore type to assign</param>
        /// <param name="targetPosition">Where the chore should be performed</param>
        /// <param name="targetCell">Grid cell for the chore</param>
        /// <param name="prefabId">Target prefab ID</param>
        /// <param name="priority">Priority level</param>
        /// <returns>True if a duplicant was found and assigned</returns>
        public static bool AssignToNearestDuplicant(Vector3 origin, float radius, string choreTypeId, 
                                                   Vector3 targetPosition, int targetCell = -1, 
                                                   string prefabId = "", float priority = 5.0f)
        {
            if (!MultiplayerSession.IsHost) return false;

            var nearestDuplicant = FindNearestAvailableDuplicant(origin, radius);
            if (nearestDuplicant == null) return false;

            var netId = nearestDuplicant.GetComponent<NetworkIdentity>()?.NetId;
            if (netId.HasValue)
            {
                AssignChore(netId.Value, choreTypeId, targetPosition, targetCell, prefabId, priority);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the nearest available duplicant within a radius.
        /// </summary>
        private static GameObject FindNearestAvailableDuplicant(Vector3 origin, float radius)
        {
            GameObject nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var identity in NetworkIdentityRegistry.GetAllIdentities())
            {
                if (identity?.gameObject == null) continue;

                // Check if it's a duplicant
                var minionIdentity = identity.gameObject.GetComponent<MinionIdentity>();
                if (minionIdentity == null) continue;

                // Check if duplicant is alive and available
                var choreDriver = identity.gameObject.GetComponent<ChoreDriver>();
                if (choreDriver == null) continue;

                // Calculate distance
                float distance = Vector3.Distance(origin, identity.gameObject.transform.position);
                if (distance <= radius && distance < nearestDistance)
                {
                    nearest = identity.gameObject;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Checks if a duplicant is available for new chore assignments.
        /// </summary>
        public static bool IsDuplicantAvailable(int duplicantNetId)
        {
            if (!NetworkIdentityRegistry.TryGet(duplicantNetId, out var identity))
                return false;

            var choreDriver = identity.gameObject.GetComponent<ChoreDriver>();
            if (choreDriver == null) return false;

            var currentChore = choreDriver.GetCurrentChore();
            
            // Available if no current chore or current chore is interruptible
            return currentChore == null || currentChore.isInterruptible;
        }

        /// <summary>
        /// Gets the current chore type ID for a duplicant, or null if idle.
        /// </summary>
        public static string GetCurrentChoreType(int duplicantNetId)
        {
            if (!NetworkIdentityRegistry.TryGet(duplicantNetId, out var identity))
                return null;

            var choreDriver = identity.gameObject.GetComponent<ChoreDriver>();
            var currentChore = choreDriver?.GetCurrentChore();
            
            return currentChore?.choreType?.Id;
        }
    }
}
