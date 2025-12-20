using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using System;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	/// <summary>
	/// Sent when research completes on the host to sync the completion to clients.
	/// </summary>
	public class ResearchCompletePacket : IPacket
	{
		public string TechId;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(TechId ?? string.Empty);
		}

		public void Deserialize(BinaryReader reader)
		{
			TechId = reader.ReadString();
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost) return;
			if (Research.Instance == null) return;
			if (string.IsNullOrEmpty(TechId)) return;

			var tech = Db.Get().Techs.TryGet(TechId);
			if (tech == null) return;

			var techInstance = Research.Instance.Get(tech);
			if (techInstance == null || techInstance.IsComplete()) return;

			try
			{
				// Mark as complete (Purchased triggers the unlocks)
				techInstance.Purchased();
				
				DebugConsole.Log($"[ResearchCompletePacket] Completed research: {tech.Name}");
				
				// Refresh the research screen if open
				try
				{
					object researchScreen = null;
					if (ManagementMenu.Instance != null)
					{
						researchScreen = HarmonyLib.Traverse.Create(ManagementMenu.Instance)
							.Field("researchScreen")
							.GetValue();
					}
					
					if (researchScreen != null)
					{
						// Call ResearchCompleted on the entry
						HarmonyLib.Traverse.Create(researchScreen)
							.Method("OnActiveResearchChanged", new Type[] { typeof(object) })
							.GetValue(null);
					}
				}
				catch { }
			}
			catch (Exception ex)
			{
				DebugConsole.LogError($"[ResearchCompletePacket] Failed to complete research: {ex.Message}");
			}
		}
	}
}
