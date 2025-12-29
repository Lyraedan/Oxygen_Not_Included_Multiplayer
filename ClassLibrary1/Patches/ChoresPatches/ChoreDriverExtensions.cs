using ONI_MP.DebugTools;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ONI_MP.Patches.Chores
{
	public static class ChoreAssignmentExtensions
	{
        public class MPChoreVars
        {
			// Any more custom variables we want to add to Chore
            public bool CanBegin = false;
        }

        private static readonly ConditionalWeakTable<Chore, MPChoreVars> mpVars = new ConditionalWeakTable<Chore, MPChoreVars>();
        public static MPChoreVars MP(this Chore chore)
        {
            if (chore == null)
                return null;

            return mpVars.GetOrCreateValue(chore);
        }

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
				if (driver.HasChore())
				{
                    //driver.StopChore();
                    //DebugConsole.Log("[ChoreDriver] Stopped chore!");

                    var current = driver.GetCurrentChore();
					if (current != null)
					{
						current.Cancel("Override chore from MP");
						DebugConsole.Log($"[Chore] Cancelled {current.choreType.Id}");
					}

                    driver.StopChore();
                    DebugConsole.Log("[ChoreDriver] Stopped chore!");
                }

                // Build context and begin
                var state = new ChoreConsumerState(consumer);
				var context = new Chore.Precondition.Context(newChore, state, true);

				// Need a thing here to allow processing in Begin
				newChore.MP().CanBegin = true;
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
