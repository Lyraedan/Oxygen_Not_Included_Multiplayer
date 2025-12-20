using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.Social
{
	public class ImmigrantOptionsPacket : IPacket
	{
		public struct OptionEntry
		{
			public bool IsDuplicant;

			// Duplicant Data
			public string Name;
			public string GenderStringKey;
			public string PersonalityId;
			
			// Traits (stored as IDs)
			public List<string> TraitIds;
			public string StressTraitId;
			public string JoyTraitId;
			public string CongenitalTraitId;
			
			// Other stats
			public int VoiceIdx;
			public string StickerType;
			
			// Skill aptitudes (SkillGroup ID -> float)
			public Dictionary<string, float> SkillAptitudes;
			
			// Starting levels (Attribute ID -> int)
			public Dictionary<string, int> StartingLevels;

			// Care Package Data
			public string CarePackageId;
			public float Quantity;
		}

		public List<OptionEntry> Options = new List<OptionEntry>();

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Options.Count);
			foreach (var opt in Options)
			{
				writer.Write(opt.IsDuplicant);
				if (opt.IsDuplicant)
				{
					writer.Write(opt.Name ?? "Unknown");
					writer.Write(opt.GenderStringKey ?? "NB");
					writer.Write(opt.PersonalityId ?? "Hassan");
					
					// Traits list
					int traitCount = opt.TraitIds?.Count ?? 0;
					writer.Write(traitCount);
					if (opt.TraitIds != null)
					{
						foreach (var traitId in opt.TraitIds)
						{
							writer.Write(traitId ?? "");
						}
					}
					
					// Special traits
					writer.Write(opt.StressTraitId ?? "");
					writer.Write(opt.JoyTraitId ?? "");
					writer.Write(opt.CongenitalTraitId ?? "");
					
					// Other stats
					writer.Write(opt.VoiceIdx);
					writer.Write(opt.StickerType ?? "");
					
					// Skill aptitudes
					int aptCount = opt.SkillAptitudes?.Count ?? 0;
					writer.Write(aptCount);
					if (opt.SkillAptitudes != null)
					{
						foreach (var kvp in opt.SkillAptitudes)
						{
							writer.Write(kvp.Key ?? "");
							writer.Write(kvp.Value);
						}
					}
					
					// Starting levels
					int levelCount = opt.StartingLevels?.Count ?? 0;
					writer.Write(levelCount);
					if (opt.StartingLevels != null)
					{
						foreach (var kvp in opt.StartingLevels)
						{
							writer.Write(kvp.Key ?? "");
							writer.Write(kvp.Value);
						}
					}
				}
				else
				{
					writer.Write(opt.CarePackageId ?? "None");
					writer.Write(opt.Quantity);
				}
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			Options = new List<OptionEntry>();
			for (int i = 0; i < count; i++)
			{
				var opt = new OptionEntry();
				opt.IsDuplicant = reader.ReadBoolean();
				if (opt.IsDuplicant)
				{
					opt.Name = reader.ReadString();
					opt.GenderStringKey = reader.ReadString();
					opt.PersonalityId = reader.ReadString();
					
					// Traits list
					int traitCount = reader.ReadInt32();
					opt.TraitIds = new List<string>();
					for (int t = 0; t < traitCount; t++)
					{
						opt.TraitIds.Add(reader.ReadString());
					}
					
					// Special traits
					opt.StressTraitId = reader.ReadString();
					opt.JoyTraitId = reader.ReadString();
					opt.CongenitalTraitId = reader.ReadString();
					
					// Other stats
					opt.VoiceIdx = reader.ReadInt32();
					opt.StickerType = reader.ReadString();
					
					// Skill aptitudes
					int aptCount = reader.ReadInt32();
					opt.SkillAptitudes = new Dictionary<string, float>();
					for (int a = 0; a < aptCount; a++)
					{
						string key = reader.ReadString();
						float val = reader.ReadSingle();
						opt.SkillAptitudes[key] = val;
					}
					
					// Starting levels
					int levelCount = reader.ReadInt32();
					opt.StartingLevels = new Dictionary<string, int>();
					for (int l = 0; l < levelCount; l++)
					{
						string key = reader.ReadString();
						int val = reader.ReadInt32();
						opt.StartingLevels[key] = val;
					}
				}
				else
				{
					opt.CarePackageId = reader.ReadString();
					opt.Quantity = reader.ReadSingle();
				}
				Options.Add(opt);
			}
		}

		public void OnDispatched()
		{
			DebugConsole.Log($"[ImmigrantOptionsPacket] Received {Options.Count} options");

			// Log each option for debugging
			for (int i = 0; i < Options.Count; i++)
			{
				var opt = Options[i];
				if (opt.IsDuplicant)
				{
					DebugConsole.Log($"[ImmigrantOptionsPacket]   Option {i}: Duplicant '{opt.Name}' (Personality: {opt.PersonalityId})");
				}
				else
				{
					DebugConsole.Log($"[ImmigrantOptionsPacket]   Option {i}: CarePackage '{opt.CarePackageId}' x{opt.Quantity}");
				}
			}

			// Check if options are already locked (first-opener-wins)
			if (ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.OptionsLocked)
			{
				DebugConsole.Log("[ImmigrantOptionsPacket] Options already locked, ignoring packet");
				return;
			}

			// Store and lock options
			ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.AvailableOptions = Options;
			ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.OptionsLocked = true;

			if (MultiplayerSession.IsHost)
			{
				// Host received from client - rebroadcast to all clients
				DebugConsole.Log("[ImmigrantOptionsPacket] Host received options from client, rebroadcasting to all clients");
				PacketSender.SendToAllClients(this);
			}

			// If the screen is already open, refresh it immediately
			if (ImmigrantScreen.instance != null && ImmigrantScreen.instance.gameObject.activeInHierarchy)
			{
				DebugConsole.Log("[ImmigrantOptionsPacket] ImmigrantScreen is open, applying options immediately");
				ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.ApplyOptionsToScreen(ImmigrantScreen.instance);
			}
			else
			{
				DebugConsole.Log("[ImmigrantOptionsPacket] ImmigrantScreen is not open, options stored for later");
			}
		}
	}
}
