using System.Collections.Generic;
using System.Linq;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Research;
using UnityEngine;

namespace ONI_MP.Misc.Research
{
    /// <summary>
    /// Manages research and skills synchronization across all multiplayer clients.
    /// Coordinates technology unlocks, research progress, and duplicant skill development.
    /// </summary>
    public static class ResearchSkillsManager
    {
        private static readonly Queue<ResearchProgressPacket> researchQueue = new Queue<ResearchProgressPacket>();
        private static readonly Queue<SkillPointsPacket> skillQueue = new Queue<SkillPointsPacket>();
        private static readonly Queue<TechnologyUnlockPacket> unlockQueue = new Queue<TechnologyUnlockPacket>();
        
        private static float researchUpdateTimer = 0f;
        private static float skillUpdateTimer = 0f;
        private static float unlockUpdateTimer = 0f;
        
        private const float ResearchUpdateInterval = 5f; // Update research progress every 5 seconds
        private const float SkillUpdateInterval = 3f; // Update skill changes every 3 seconds
        private const float UnlockUpdateInterval = 1f; // Update unlocks immediately (1 second)

        // Track last known state to detect changes
        private static Dictionary<string, float> lastResearchProgress = new Dictionary<string, float>();
        private static Dictionary<int, float> lastDuplicantSkillPoints = new Dictionary<int, float>();
        private static HashSet<string> lastUnlockedTechs = new HashSet<string>();

        public static void QueueResearchProgress(string techId, float points, bool isCompleted, 
                                               float totalRequired, string researcherDuplicantNetId = "", float speed = 1f)
        {
            if (!MultiplayerSession.IsHost) return;

            lock (researchQueue)
            {
                researchQueue.Enqueue(new ResearchProgressPacket
                {
                    TechId = techId,
                    ResearchPoints = points,
                    IsCompleted = isCompleted,
                    TotalPointsRequired = totalRequired,
                    ResearcherDuplicantNetId = researcherDuplicantNetId,
                    ResearchSpeed = speed
                });
            }
        }

        public static void QueueSkillUpdate(int duplicantNetId, float availablePoints, float totalExp, 
                                          List<string> learnedSkills, string duplicantName = "")
        {
            if (!MultiplayerSession.IsHost) return;

            lock (skillQueue)
            {
                var packet = new SkillPointsPacket
                {
                    DuplicantNetId = duplicantNetId,
                    AvailableSkillPoints = availablePoints,
                    TotalExperience = totalExp,
                    LearnedSkills = new List<string>(learnedSkills),
                    DuplicantName = duplicantName
                };
                
                skillQueue.Enqueue(packet);
            }
        }

        public static void QueueTechnologyUnlock(string techId, string playerSteamId = "")
        {
            if (!MultiplayerSession.IsHost) return;

            var tech = Db.Get().Techs.TryGet(techId);
            if (tech == null) return;

            lock (unlockQueue)
            {
                var packet = TechnologyUnlockPacket.FromTechnology(tech, playerSteamId);
                if (packet != null)
                {
                    unlockQueue.Enqueue(packet);
                }
            }
        }

        public static void Update()
        {
            if (!MultiplayerSession.IsHost) return;

            // Update research progress
            researchUpdateTimer += Time.unscaledDeltaTime;
            if (researchUpdateTimer >= ResearchUpdateInterval)
            {
                CheckAndSyncResearchProgress();
                FlushResearchProgress();
                researchUpdateTimer = 0f;
            }

            // Update skill changes
            skillUpdateTimer += Time.unscaledDeltaTime;
            if (skillUpdateTimer >= SkillUpdateInterval)
            {
                CheckAndSyncDuplicantSkills();
                FlushSkillUpdates();
                skillUpdateTimer = 0f;
            }

            // Update technology unlocks
            unlockUpdateTimer += Time.unscaledDeltaTime;
            if (unlockUpdateTimer >= UnlockUpdateInterval)
            {
                CheckAndSyncTechnologyUnlocks();
                FlushTechnologyUnlocks();
                unlockUpdateTimer = 0f;
            }
        }

        private static void CheckAndSyncResearchProgress()
        {
            // Note: Research.Instance may not be directly accessible
            // For now we skip automatic progress checking and rely on patches
            DebugConsole.Log("[Research & Skills] Research progress check (placeholder)");
        }

        private static void CheckAndSyncDuplicantSkills()
        {
            var duplicants = UnityEngine.Object.FindObjectsOfType<MinionResume>();
            
            foreach (var resume in duplicants)
            {
                var identity = resume.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                var netId = identity.NetId;
                var currentSkillPoints = resume.AvailableSkillpoints;

                // Check if skill points have changed
                if (!lastDuplicantSkillPoints.ContainsKey(netId) || 
                    Mathf.Abs(lastDuplicantSkillPoints[netId] - currentSkillPoints) > 0.01f)
                {
                    lastDuplicantSkillPoints[netId] = currentSkillPoints;
                    
                    var learnedSkills = resume.MasteryBySkillID?.Keys.ToList() ?? new List<string>();
                    var minionIdentity = resume.GetComponent<MinionIdentity>();
                    
                    QueueSkillUpdate(netId, currentSkillPoints, resume.TotalExperienceGained, 
                                   learnedSkills, minionIdentity?.name ?? "Unknown");
                }
            }
        }

        private static void CheckAndSyncTechnologyUnlocks()
        {
            // Note: Research.Instance may not be directly accessible
            // For now we skip automatic unlock checking and rely on patches
            DebugConsole.Log("[Research & Skills] Technology unlock check (placeholder)");
        }

        private static void FlushResearchProgress()
        {
            lock (researchQueue)
            {
                if (researchQueue.Count == 0) return;

                int count = 0;
                while (researchQueue.Count > 0 && count < 10) // Limit to 10 research updates per flush
                {
                    var packet = researchQueue.Dequeue();
                    PacketSender.SendToAll(packet);
                    count++;
                }

                if (count > 0)
                {
                    DebugConsole.Log($"[Research & Skills] Synchronized {count} research progress updates");
                }
            }
        }

        private static void FlushSkillUpdates()
        {
            lock (skillQueue)
            {
                if (skillQueue.Count == 0) return;

                int count = 0;
                while (skillQueue.Count > 0 && count < 20) // Limit to 20 skill updates per flush
                {
                    var packet = skillQueue.Dequeue();
                    PacketSender.SendToAll(packet);
                    count++;
                }

                if (count > 0)
                {
                    DebugConsole.Log($"[Research & Skills] Synchronized {count} duplicant skill updates");
                }
            }
        }

        private static void FlushTechnologyUnlocks()
        {
            lock (unlockQueue)
            {
                if (unlockQueue.Count == 0) return;

                int count = 0;
                while (unlockQueue.Count > 0 && count < 5) // Limit to 5 unlocks per flush
                {
                    var packet = unlockQueue.Dequeue();
                    PacketSender.SendToAll(packet);
                    count++;
                }

                if (count > 0)
                {
                    DebugConsole.Log($"[Research & Skills] Synchronized {count} technology unlocks");
                }
            }
        }

        /// <summary>
        /// Force sync all research and skills state (used during game sync)
        /// </summary>
        public static void ForceFullSync()
        {
            if (!MultiplayerSession.IsHost) return;

            DebugConsole.Log("[Research & Skills] Performing full research and skills sync");

            // Clear tracking state to force re-sync
            lastResearchProgress.Clear();
            lastDuplicantSkillPoints.Clear();
            lastUnlockedTechs.Clear();

            // Immediately check and sync everything
            CheckAndSyncResearchProgress();
            CheckAndSyncDuplicantSkills();
            CheckAndSyncTechnologyUnlocks();

            // Flush all queues
            FlushResearchProgress();
            FlushSkillUpdates();
            FlushTechnologyUnlocks();
        }

        /// <summary>
        /// Get current research statistics for debugging
        /// </summary>
        public static string GetResearchStats()
        {
            // Note: Research.Instance may not be directly accessible
            return "Research stats: API access limited";
        }
    }
}
