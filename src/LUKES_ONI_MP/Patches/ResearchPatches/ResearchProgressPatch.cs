using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc.Research;
using ONI_MP.Networking;

namespace ONI_MP.Patches.ResearchPatches
{
    /// <summary>
    /// Patches Research class to synchronize research progress across multiplayer clients
    /// </summary>
    public static class ResearchProgressPatch
    {
        // Patch when technology is purchased/completed
        [HarmonyPatch(typeof(TechInstance), nameof(TechInstance.Purchased))]
        [HarmonyPostfix]
        public static void Purchased_Postfix(TechInstance __instance)
        {
            if (!MultiplayerSession.IsHost) return;

            // Queue technology unlock
            ResearchSkillsManager.QueueTechnologyUnlock(
                __instance.tech.Id, 
                MultiplayerSession.LocalSteamID.ToString()
            );

            DebugConsole.Log($"[Research & Skills] Technology completed: {__instance.tech.Name}");
        }

        // Patch research screen updates to sync when research target changes
        [HarmonyPatch(typeof(Research), nameof(Research.SetActiveResearch))]
        [HarmonyPostfix]
        public static void SetActiveResearch_Postfix(Research __instance, Tech tech)
        {
            if (!MultiplayerSession.IsHost) return;

            if (tech != null)
            {
                var techInstance = __instance.Get(tech);
                if (techInstance != null)
                {
                    var totalProgress = 0f;
                    if (techInstance.progressInventory?.PointsByTypeID != null)
                    {
                        foreach (var kvp in techInstance.progressInventory.PointsByTypeID)
                        {
                            totalProgress += kvp.Value;
                        }
                    }

                    var totalRequired = 0f;
                    if (tech.costsByResearchTypeID != null)
                    {
                        foreach (var kvp in tech.costsByResearchTypeID)
                        {
                            totalRequired += kvp.Value;
                        }
                    }

                    // Sync current research target progress
                    ResearchSkillsManager.QueueResearchProgress(
                        tech.Id, 
                        totalProgress, 
                        techInstance.IsComplete(), 
                        totalRequired
                    );
                }

                DebugConsole.Log($"[Research & Skills] Active research set to: {tech.Name}");
            }
        }
    }
}
