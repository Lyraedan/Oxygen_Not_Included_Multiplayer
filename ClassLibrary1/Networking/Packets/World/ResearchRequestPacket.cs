using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using System;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	public class ResearchRequestPacket : IPacket
	{
		public string TechId { get; set; }

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
			if (!MultiplayerSession.IsHost) return;

			// Host received request from client
			if (!string.IsNullOrEmpty(TechId))
			{
				var tech = Db.Get().Techs.TryGet(TechId);
				if (tech != null)
				{
					// Get the ResearchScreen for visual updates on host
					object researchScreen = null;
					if (ManagementMenu.Instance != null)
					{
						researchScreen = HarmonyLib.Traverse.Create(ManagementMenu.Instance)
							.Field("researchScreen")
							.GetValue();
					}
					
					// First, deselect all current queue items visually
					try
					{
						var queueField = HarmonyLib.AccessTools.Field(typeof(Research), "queuedTech");
						if (queueField != null)
						{
							var localQueue = queueField.GetValue(Research.Instance) as System.Collections.IList;
							if (localQueue != null && localQueue.Count > 0 && researchScreen != null)
							{
								foreach (var item in localQueue)
								{
									var techInstance = item as TechInstance;
									if (techInstance?.tech != null)
									{
										try
										{
											HarmonyLib.Traverse.Create(researchScreen)
												.Method("SelectAllEntries", new Type[] { typeof(Tech), typeof(bool) })
												.GetValue(techInstance.tech, false);
										}
										catch { }
									}
								}
							}
						}
					}
					catch { }
					
					// Set the new research
					Research.Instance.SetActiveResearch(tech, true);
					
					// Select the new research visually on host
					if (researchScreen != null)
					{
						try
						{
							HarmonyLib.Traverse.Create(researchScreen)
								.Method("SelectAllEntries", new Type[] { typeof(Tech), typeof(bool) })
								.GetValue(tech, true);
						}
						catch { }
					}
					
					// ResearchPatch will trigger and sync back to all clients
				}
			}
		}
	}
}

