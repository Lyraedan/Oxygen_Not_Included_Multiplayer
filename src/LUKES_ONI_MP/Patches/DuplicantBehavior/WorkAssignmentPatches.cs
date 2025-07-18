using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc.DuplicantBehavior;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using UnityEngine;

namespace ONI_MP.Patches.DuplicantBehavior
{
    /// <summary>
    /// Patches for work assignment synchronization.
    /// Intercepts chore assignments and coordinates them across multiplayer clients.
    /// </summary>
    public static class WorkAssignmentPatches
    {
        [HarmonyPatch(typeof(ChoreDriver), nameof(ChoreDriver.SetChore))]
        public static class ChoreDriver_SetChore_Patch
        {
            public static void Postfix(ChoreDriver __instance, Chore chore)
            {
                if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost) return;

                var identity = __instance.GetComponent<NetworkIdentity>();
                if (identity == null) return;

                if (chore != null && chore.choreType != null)
                {
                    var targetPos = chore.gameObject?.transform.position ?? Vector3.zero;
                    var targetCell = Grid.PosToCell(chore.gameObject);
                    var prefabId = chore.gameObject?.PrefabID()?.Name ?? "";

                    DuplicantBehaviorManager.QueueWorkAssignment(
                        identity.NetId,
                        chore.choreType.Id,
                        targetPos,
                        targetCell,
                        prefabId,
                        5.0f, // Default priority
                        DetermineWorkerRole(chore.choreType.Id),
                        IsUrgentChore(chore.choreType.Id)
                    );

                    DebugConsole.Log($"[WorkAssignmentPatch] Queued work assignment: {chore.choreType.Id} for duplicant {identity.NetId}");
                }
            }

            private static string DetermineWorkerRole(string choreTypeId)
            {
                return choreTypeId switch
                {
                    "Dig" or "DigPlacer" => "Mining",
                    "Build" or "Construct" => "Construction",
                    "Repair" => "Maintenance",
                    "Cook" => "Cooking",
                    "Research" => "Research",
                    "Harvest" => "Farming",
                    "Rancher" => "Ranching",
                    "Doctor" => "Medical",
                    "Art" => "Art",
                    _ => "General"
                };
            }

            private static bool IsUrgentChore(string choreTypeId)
            {
                return choreTypeId switch
                {
                    "Flee" or "MoveToSafety" or "RecoverBreath" or "Die" => true,
                    "Repair" => true, // Repairs are often urgent
                    "Doctor" => true, // Medical tasks are urgent
                    _ => false
                };
            }
        }

        [HarmonyPatch(typeof(Chore), nameof(Chore.Cancel))]
        public static class Chore_Cancel_Patch
        {
            public static void Postfix(Chore __instance, string reason)
            {
                if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost) return;

                var driver = __instance.driver;
                if (driver == null) return;

                var identity = driver.GetComponent<NetworkIdentity>();
                if (identity == null) return;

                // Queue a "None" work assignment to indicate chore cancellation
                DuplicantBehaviorManager.QueueWorkAssignment(
                    identity.NetId,
                    "None",
                    Vector3.zero,
                    -1,
                    "",
                    0f,
                    "",
                    false
                );

                DebugConsole.Log($"[WorkAssignmentPatch] Queued work cancellation for duplicant {identity.NetId}: {reason}");
            }
        }

        [HarmonyPatch(typeof(ChoreConsumer), nameof(ChoreConsumer.IsPermittedByUser))]
        public static class ChoreConsumer_IsPermittedByUser_Patch
        {
            public static void Postfix(ChoreConsumer __instance, ChoreType chore_type, bool __result)
            {
                if (!MultiplayerSession.InSession || !__result) return;

                var identity = __instance.GetComponent<NetworkIdentity>();
                if (identity == null) return;

                // This patch helps track what chores duplicants are allowed to do
                // Could be used for coordinating work preferences across clients
                DebugConsole.Log($"[WorkAssignmentPatch] Duplicant {identity.NetId} permitted to do {chore_type.Id}");
            }
        }
    }
}
