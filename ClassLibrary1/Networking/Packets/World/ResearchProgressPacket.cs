using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.World
{
	/// <summary>
	/// Syncs research progress percentage from host to clients.
	/// Sent periodically to keep progress bars in sync.
	/// </summary>
	public class ResearchProgressPacket : IPacket
	{

		public string TechId;
		public float Progress; // 0.0 to 1.0

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(TechId ?? string.Empty);
			writer.Write(Progress);
		}

		public void Deserialize(BinaryReader reader)
		{
			TechId = reader.ReadString();
			Progress = reader.ReadSingle();
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost) return;
			if (Research.Instance == null) return;
			if (string.IsNullOrEmpty(TechId)) return;

			var tech = Db.Get().Techs.TryGet(TechId);
			if (tech == null) return;

			var techInstance = Research.Instance.Get(tech);
			if (techInstance == null) return;

			// Set the progress on each research type via reflection
			try
			{
				var pointsDict = techInstance.progressInventory.PointsByTypeID;
				
				if (pointsDict != null)
				{
					foreach (var researchType in tech.costsByResearchTypeID.Keys)
					{
						float cost = tech.costsByResearchTypeID[researchType];
						float newPoints = cost * Progress;
						
						pointsDict[researchType] = Mathf.RoundToInt(newPoints);
					}
				}
				
				// Refresh the research screen if open
				try
				{
					object researchScreen = null;
					if (ManagementMenu.Instance != null)
					{
						researchScreen = ManagementMenu.Instance.researchScreen;
					}
					
					if (researchScreen != null)
					{
						HarmonyLib.Traverse.Create(researchScreen)
							.Method("UpdateProgressBars")
							.GetValue();
					}
				}
				catch { }
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogWarning($"[ResearchProgressPacket] Failed to set progress: {ex.Message}");
			}
		}
	}
}

