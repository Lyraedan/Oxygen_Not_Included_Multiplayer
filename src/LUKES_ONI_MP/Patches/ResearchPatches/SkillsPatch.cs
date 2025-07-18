using HarmonyLib;
using System.Linq;
using ONI_MP.DebugTools;
using ONI_MP.Misc.Research;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;

namespace ONI_MP.Patches.ResearchPatches
{
    /// <summary>
    /// Patches MinionResume class to synchronize duplicant skills across multiplayer clients
    /// </summary>
    public static class SkillsPatch
    {
        // Patch when a duplicant masters a skill
        [HarmonyPatch(typeof(MinionResume), nameof(MinionResume.MasterSkill))]
        [HarmonyPostfix]
        public static void MasterSkill_Postfix(MinionResume __instance, string skillID)
        {
            if (!MultiplayerSession.IsHost) return;

            var identity = __instance.GetComponent<NetworkIdentity>();
            var minionIdentity = __instance.GetComponent<MinionIdentity>();
            
            if (identity != null)
            {
                var learnedSkills = __instance.MasteryBySkillID?.Keys.ToList() ?? new System.Collections.Generic.List<string>();
                
                ResearchSkillsManager.QueueSkillUpdate(
                    identity.NetId,
                    __instance.AvailableSkillpoints,
                    __instance.TotalExperienceGained,
                    learnedSkills,
                    minionIdentity?.name ?? "Unknown"
                );

                var skill = Db.Get().Skills.TryGet(skillID);
                DebugConsole.Log($"[Research & Skills] {minionIdentity?.name ?? "Duplicant"} mastered skill: {skill?.Name ?? skillID}");
            }
        }

        // Patch when skill points are spent
        [HarmonyPatch(typeof(MinionResume), nameof(MinionResume.AddExperience))]
        [HarmonyPostfix]
        public static void AddExperience_Postfix(MinionResume __instance, float amount)
        {
            if (!MultiplayerSession.IsHost) return;

            var identity = __instance.GetComponent<NetworkIdentity>();
            var minionIdentity = __instance.GetComponent<MinionIdentity>();
            
            if (identity != null && amount > 0f)
            {
                var learnedSkills = __instance.MasteryBySkillID?.Keys.ToList() ?? new System.Collections.Generic.List<string>();
                
                ResearchSkillsManager.QueueSkillUpdate(
                    identity.NetId,
                    __instance.AvailableSkillpoints,
                    __instance.TotalExperienceGained,
                    learnedSkills,
                    minionIdentity?.name ?? "Unknown"
                );

                DebugConsole.Log($"[Research & Skills] {minionIdentity?.name ?? "Duplicant"} gained {amount:F1} experience (Total: {__instance.TotalExperienceGained:F1}, Available: {__instance.AvailableSkillpoints:F1})");
            }
        }

        // Patch when skill points are directly set (for admin commands or cheats)
        [HarmonyPatch(typeof(MinionResume), "AvailableSkillpoints", MethodType.Setter)]
        [HarmonyPostfix]
        public static void AvailableSkillpoints_Set_Postfix(MinionResume __instance, float value)
        {
            if (!MultiplayerSession.IsHost) return;

            var identity = __instance.GetComponent<NetworkIdentity>();
            var minionIdentity = __instance.GetComponent<MinionIdentity>();
            
            if (identity != null)
            {
                var learnedSkills = __instance.MasteryBySkillID?.Keys.ToList() ?? new System.Collections.Generic.List<string>();
                
                ResearchSkillsManager.QueueSkillUpdate(
                    identity.NetId,
                    value,
                    __instance.TotalExperienceGained,
                    learnedSkills,
                    minionIdentity?.name ?? "Unknown"
                );

                DebugConsole.Log($"[Research & Skills] {minionIdentity?.name ?? "Duplicant"} skill points set to: {value:F1}");
            }
        }

        // Patch skill screen interactions
        [HarmonyPatch(typeof(SkillsScreen), "OnSelectMinion")]
        [HarmonyPostfix]
        public static void SkillsScreen_OnSelectMinion_Postfix(SkillsScreen __instance, MinionIdentity minion)
        {
            if (!MultiplayerSession.IsHost) return;

            if (minion != null)
            {
                var resume = minion.GetComponent<MinionResume>();
                var identity = minion.GetComponent<NetworkIdentity>();
                
                if (resume != null && identity != null)
                {
                    // Sync current skills when skill screen is opened for a duplicant
                    var learnedSkills = resume.MasteryBySkillID?.Keys.ToList() ?? new System.Collections.Generic.List<string>();
                    
                    ResearchSkillsManager.QueueSkillUpdate(
                        identity.NetId,
                        resume.AvailableSkillpoints,
                        resume.TotalExperienceGained,
                        learnedSkills,
                        minion.name
                    );
                }
            }
        }

        // Patch when unlocking a skill from the skills screen
        [HarmonyPatch(typeof(SkillWidget), "OnSkillUnlocked")]
        [HarmonyPostfix]
        public static void SkillWidget_OnSkillUnlocked_Postfix(SkillWidget __instance)
        {
            if (!MultiplayerSession.IsHost) return;

            // Note: SkillsScreen.Instance may not be accessible, so we skip this for now
            DebugConsole.Log("[Research & Skills] Skill unlocked via UI (sync placeholder)");
        }
    }
}
