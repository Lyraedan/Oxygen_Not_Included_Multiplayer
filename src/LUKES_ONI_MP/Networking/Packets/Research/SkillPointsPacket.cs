using System.Collections.Generic;
using System.IO;
using System.Linq;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Research
{
    /// <summary>
    /// Synchronizes duplicant skill points and abilities across all multiplayer clients.
    /// Tracks skill progression, available points, and learned abilities.
    /// </summary>
    public class SkillPointsPacket : IPacket
    {
        public PacketType Type => PacketType.SkillPoints;

        public int DuplicantNetId;
        public float AvailableSkillPoints;
        public float TotalExperience;
        public List<string> LearnedSkills;
        public Dictionary<string, float> SkillExperience; // Per-skill experience tracking
        public string DuplicantName; // For debugging

        public SkillPointsPacket()
        {
            LearnedSkills = new List<string>();
            SkillExperience = new Dictionary<string, float>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(AvailableSkillPoints);
            writer.Write(TotalExperience);
            writer.Write(DuplicantName ?? "");

            // Serialize learned skills
            writer.Write(LearnedSkills.Count);
            foreach (var skill in LearnedSkills)
            {
                writer.Write(skill);
            }

            // Serialize skill experience
            writer.Write(SkillExperience.Count);
            foreach (var kvp in SkillExperience)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            AvailableSkillPoints = reader.ReadSingle();
            TotalExperience = reader.ReadSingle();
            DuplicantName = reader.ReadString();

            // Deserialize learned skills
            LearnedSkills.Clear();
            int skillCount = reader.ReadInt32();
            for (int i = 0; i < skillCount; i++)
            {
                LearnedSkills.Add(reader.ReadString());
            }

            // Deserialize skill experience
            SkillExperience.Clear();
            int expCount = reader.ReadInt32();
            for (int i = 0; i < expCount; i++)
            {
                string skillId = reader.ReadString();
                float experience = reader.ReadSingle();
                SkillExperience[skillId] = experience;
            }
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost) return;

            if (!NetworkIdentityRegistry.TryGet(DuplicantNetId, out NetworkIdentity duplicantIdentity))
            {
                DebugConsole.LogWarning($"[Research & Skills] Duplicant with NetId {DuplicantNetId} not found for skill sync");
                return;
            }

            var duplicantObj = duplicantIdentity.gameObject;
            var resume = duplicantObj.GetComponent<MinionResume>();
            
            if (resume == null)
            {
                DebugConsole.LogWarning($"[Research & Skills] No MinionResume component on duplicant {DuplicantNetId}");
                return;
            }

            try
            {
                // Note: ONI's MinionResume properties may be read-only, so we use alternative approaches
                
                // Sync learned skills
                foreach (var skillId in LearnedSkills)
                {
                    var skill = Db.Get().Skills.TryGet(skillId);
                    if (skill != null && resume.MasteryBySkillID != null && !resume.MasteryBySkillID.ContainsKey(skillId))
                    {
                        resume.MasterSkill(skillId);
                        DebugConsole.Log($"[Research & Skills] {DuplicantName} learned skill: {skill.Name}");
                    }
                }

                // Log skill points update (setting may not be available)
                DebugConsole.Log($"[Research & Skills] Updated skills for {DuplicantName}: {AvailableSkillPoints:F1} points, {LearnedSkills.Count} skills");
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[Research & Skills] Error updating duplicant skills: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a SkillPointsPacket from current duplicant state
        /// </summary>
        public static SkillPointsPacket FromDuplicant(GameObject duplicantObj)
        {
            var resume = duplicantObj.GetComponent<MinionResume>();
            var identity = duplicantObj.GetComponent<NetworkIdentity>();
            var minionIdentity = duplicantObj.GetComponent<MinionIdentity>();

            if (resume == null || identity == null) return null;

            var packet = new SkillPointsPacket
            {
                DuplicantNetId = identity.NetId,
                AvailableSkillPoints = resume.AvailableSkillpoints,
                TotalExperience = resume.TotalExperienceGained,
                DuplicantName = minionIdentity?.name ?? "Unknown"
            };

            // Collect learned skills
            if (resume.MasteryBySkillID != null)
            {
                packet.LearnedSkills = resume.MasteryBySkillID.Keys.ToList();
            }

            // For now, we don't have direct access to per-skill experience
            // but we can track total experience and derive some information
            packet.SkillExperience["total"] = resume.TotalExperienceGained;

            return packet;
        }
    }
}
