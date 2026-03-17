using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.World.Handlers;
using ONI_MP.DebugTools;
using System.IO;
using UnityEngine;
using HarmonyLib;
using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.World
{
	public enum BuildingConfigType : byte
	{
		Float = 0,      // Standard float value (valve flow, thresholds)
		Boolean = 1,    // Checkbox values
		SliderIndex = 2, // Slider with index (for multi-slider controls)
		RecipeQueue = 3, // Fabricator recipe queue (ConfigHash = recipe ID hash, Value = count)
		String = 4       // String value (tag names, text fields)
	}

	public class BuildingConfigPacket : IPacket
	{
		public int NetId;
		public int Cell; // Deterministic location-based identification
		public int ConfigHash; // Hash of the property name (e.g. "Threshold", "Logic")
		public float Value;
		public BuildingConfigType ConfigType = BuildingConfigType.Float;
		public int SliderIndex = 0; // For ISliderControl multi-sliders
		public string StringValue = ""; // For tag names and text fields

		public static bool IsApplyingPacket = false;

		public void Serialize(BinaryWriter writer)
		{
			Profiler.Active.Scope();

			writer.Write(NetId);
			writer.Write(Cell);
			writer.Write(ConfigHash);
			writer.Write(Value);
			writer.Write((byte)ConfigType);
			writer.Write(SliderIndex);
			writer.Write(StringValue ?? "");
		}

		public void Deserialize(BinaryReader reader)
		{
			Profiler.Active.Scope();

			NetId = reader.ReadInt32();
			Cell = reader.ReadInt32();
			ConfigHash = reader.ReadInt32();
			Value = reader.ReadSingle();
			ConfigType = (BuildingConfigType)reader.ReadByte();
			SliderIndex = reader.ReadInt32();
			StringValue = reader.ReadString();
		}

		public void OnDispatched()
		{
			Profiler.Active.Scope();

			DebugConsole.Log($"[BuildingConfigPacket] Received a config update packet. NetId={NetId}, Cell={Cell}");

			if (!NetworkIdentityRegistry.TryGet(NetId, out var identity) || identity == null)
			{
				// Attempt to find building by cell
				if (Grid.IsValidCell(Cell))
				{
					// For multi-layered buildings, we might need a more specific search, but usually 
					// we just look for BuildingComplete components.
					GameObject buildingGO = Grid.Objects[Cell, (int)ObjectLayer.Building];
					if (buildingGO != null)
					{
						identity = buildingGO.GetComponent<NetworkIdentity>();
						if (identity)
						{
							identity.OverrideNetId(NetId); // Override properly from the host
						}
						else
						{
							identity = buildingGO.AddOrGet<NetworkIdentity>();
							identity.NetId = NetId; // Client forces the NetId from Host
							identity.RegisterIdentity();
						}
						DebugConsole.Log($"[BuildingConfigPacket] Resolved missing identity for {buildingGO.name} at cell {Cell}. Assigned NetId: {NetId}");
					}
				}
			}

			if (identity != null)
			{
				try
				{
					IsApplyingPacket = true;
					ApplyConfig(identity.gameObject);
				}
				finally
				{
					IsApplyingPacket = false;
				}

				// UI auto-refresh disabled - each side screen subscribes to different 
				// component-specific events. No universal refresh event exists.
				// Users must close/reopen screens to see changes from other players.
				// The sync still works - only the visual update is not instant.
				// RefreshSideScreenIfOpen(identity.gameObject);

				// HOST RELAY: If host received this from a client, re-broadcast to all other clients
				if (MultiplayerSession.IsHost)
				{
					PacketSender.SendToAllClients(this);
					DebugConsole.Log($"[BuildingConfigPacket] Host relayed config to all clients: NetId={NetId}, ConfigHash={ConfigHash}");
				}
			}
			else
			{
				DebugConsole.LogWarning($"[BuildingConfigPacket] FAILED to resolve entity for NetId {NetId} at Cell {Cell}");
			}
		}

		/// <summary>
		/// Triggers a refresh event on the building so side screens update.
		/// Uses the same event ID (644822890) that components like Storage use internally.
		/// </summary>
		private void RefreshSideScreenIfOpen(GameObject go)
		{
			Profiler.Active.Scope();

			if (go == null) return;
			
			try
			{
				// Check if this building is currently selected in the details screen
				if (DetailsScreen.Instance == null) return;
				if (DetailsScreen.Instance.target != go) return;
				
				// Trigger the refresh event on the building's KMonoBehaviour
				// 644822890 is the event Storage uses for sweep-only updates
				// This event notifies side screens to refresh their display
				var kMono = go.GetComponent<KMonoBehaviour>();
				if (kMono != null)
				{
					kMono.Trigger(644822890); // Storage update event
					DebugConsole.Log($"[BuildingConfigPacket] Triggered UI refresh event on {go.name}");
				}
			}
			catch (System.Exception e)
			{
				// Non-critical - just log and continue
				DebugConsole.Log($"[BuildingConfigPacket] Side screen refresh failed: {e.Message}");
			}
		}

		/// <summary>
		/// Applies the configuration to the target building.
		/// All handlers are now in the BuildingConfigHandlerRegistry.
		/// </summary>
		private void ApplyConfig(GameObject go)
		{
			Profiler.Active.Scope();

			if (go == null) return;

			// All handlers are now in the registry
			if (BuildingConfigHandlerRegistry.TryHandle(go, this))
			{
				DebugConsole.Log($"[BuildingConfigPacket] Handled by registry for {go.name}");
				return;
			}

			// Log unhandled configs for debugging
			DebugConsole.LogWarning($"[BuildingConfigPacket] Unhandled config: Hash={ConfigHash}, Type={ConfigType}, Value={Value}, String={StringValue} on {go.name}");
		}
	}
}
