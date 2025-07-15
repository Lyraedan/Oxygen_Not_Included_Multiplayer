using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ONI_MP.DebugTools;

namespace ONI_MP.Patches.Chores
{
    public static class ChoreAssignmentExtensions
    {
        public static void AssignChoreToDuplicant(this Chore newChore, GameObject dupeGO)
        {
            try
            {
                if (newChore == null || dupeGO == null || !newChore.IsValid())
                {
                    DebugConsole.LogWarning("[ChoreAssignment] Invalid chore or duplicant.");
                    return;
                }

                var consumer = dupeGO.GetComponent<ChoreConsumer>();
                if (consumer == null || consumer.choreDriver == null)
                {
                    DebugConsole.LogWarning("[ChoreAssignment] Missing ChoreConsumer or ChoreDriver.");
                    return;
                }

                var driver = consumer.choreDriver;

                // Cancel current chore
                var current = driver.GetCurrentChore();
                if (current != null)
                {
                    current.Cancel("Override chore from MP");
                }

                // Build context and begin
                var state = new ChoreConsumerState(consumer);
                var context = new Chore.Precondition.Context(newChore, state, true);

                newChore.Begin(context);

                DebugConsole.Log($"[ChoreAssignment] Assigned chore {newChore.choreType.Id} to {dupeGO.name}");
            }
            catch (Exception ex)
            {
                DebugConsole.LogException(ex);
            }
        }
    }
}
