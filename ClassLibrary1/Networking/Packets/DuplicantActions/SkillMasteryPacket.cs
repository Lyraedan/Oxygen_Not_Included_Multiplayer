using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using System.IO;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
	public class SkillMasteryPacket : IPacket
	{
		public int NetId;
		public string SkillId;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(SkillId ?? string.Empty);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			SkillId = reader.ReadString();
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
			{
				Apply();
				PacketSender.SendToAllClients(this);
			}
			else
			{
				Apply();
			}
		}

		private void Apply()
		{
			if (!NetworkIdentityRegistry.TryGet(NetId, out var identity))
			{
				DebugConsole.LogWarning($"[SkillMasteryPacket] NetId {NetId} not found.");
				return;
			}

			var resume = identity.GetComponent<MinionResume>();
			if (resume == null)
			{
				DebugConsole.LogWarning($"[SkillMasteryPacket] NetId {NetId} has no MinionResume.");
				return;
			}

			if (Db.Get().Skills.TryGet(SkillId) == null)
			{
				DebugConsole.LogWarning($"[SkillMasteryPacket] SkillId {SkillId} not found.");
				return;
			}

			if (resume.HasMasteredSkill(SkillId))
			{
				return;
			}

			IsApplying = true;
			try
			{
				// Force mastery. MinionResume.MasterSkill(skillId) usually checks points.
				// We might need to bypass checks or ensure points are available?
				// For sync, we assume the sender validated it.
				// We want to force it.

				// resume.MasterSkill(SkillId); // This deducts points and triggers effects.

				// If points are desynced, this might fail or create negative points.
				// Ideally we sync experience/points too. 
				// But for now, let's call the game method.

				resume.MasterSkill(SkillId); // This deducts points and triggers effects.

				DebugConsole.Log($"[SkillMasteryPacket] Applied Skill {SkillId} to {identity.name}");
			}
			finally
			{
				IsApplying = false;
			}
		}

		public static bool IsApplying = false;
	}
}
