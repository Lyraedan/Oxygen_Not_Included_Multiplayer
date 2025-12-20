using ONI_MP.Api.Networking;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Trackers;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class WorldStateSyncer : MonoBehaviour
	{
		public static WorldStateSyncer Instance { get; private set; }

		private const float SYNC_INTERVAL = 5f; // Faster sync for other things
		private float _lastSyncTime;

		// Gas/Liquid Sync
		private float _lastGasSyncTime;
		private const float GAS_SYNC_INTERVAL = 0.2f; // 5 FPS for gas/liquid visual changes

		private ushort[] _shadowElements;
		private float[] _shadowMass;

		private readonly Dictionary<Steamworks.CSteamID, RectInt> _clientViewports = new Dictionary<Steamworks.CSteamID, RectInt>();

		private void Awake()
		{
			Instance = this;
		}

		public void UpdateClientView(Steamworks.CSteamID steamId, int minX, int minY, int maxX, int maxY)
		{
			// Update or add
			_clientViewports[steamId] = new RectInt(minX, minY, maxX - minX, maxY - minY);
		}

		private void Update()
		{
			if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost)
				return;

			try
			{
				if (Time.unscaledTime - _lastGasSyncTime > GAS_SYNC_INTERVAL)
				{
					_lastGasSyncTime = Time.unscaledTime;
					SyncGasLiquid();
				}

				if (Time.unscaledTime - _lastSyncTime > SYNC_INTERVAL)
				{
					_lastSyncTime = Time.unscaledTime;
					SyncDigging();
					SyncChores();
					SyncResearchProgress();
					// SyncResearch() - REMOVED: Research is now synced only when selected (via ResearchPatch/ResearchRequestPacket)
					SyncPriorities();
					SyncDisinfectImpl();
				}
			}
			catch (System.Exception)
			{
				// Silently ignore - sync may fail on freshly loaded world
			}
		}

		// --- Digging Logic ---

		private void SyncDigging()
		{
			var digPacket = new DiggingStatePacket();

			// Efficient scan: Iterate DigPlacers? 
			// ONI doesn't expose a global list of DigPlacers easily.
			// But we know DigPlacers have a 'Diggable' component.
			// Diggable.GetDiggable(cell) is a lookup.
			// We might have to scan the Grid or keep track.
			// Scanning the whole grid is heavy.
			// Better: Components.Diggables.Items

			try
			{
				foreach (var diggable in global::Components.Diggables.Items)
				{
					if (diggable == null) continue;
					int cell = Grid.PosToCell(diggable);
					if (Grid.IsValidCell(cell))
					{
						digPacket.DigCells.Add(cell);
					}
				}

				PacketSender.SendToAllClients(digPacket, SteamNetworkingSend.Unreliable);
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[WorldStateSyncer] Error in SyncDigging: {ex.Message}");
			}
		}

		public void OnDiggingStateReceived(DiggingStatePacket packet)
		{
			// Reconcile
			// 1. Get all local diggables
			// 2. Remove extra
			// 3. Add missing

			try
			{
				var localDigs = new HashSet<int>();
				var toRemove = new List<Diggable>();

				foreach (var diggable in global::Components.Diggables.Items)
				{
					int cell = Grid.PosToCell(diggable);
					localDigs.Add(cell);
					if (!packet.DigCells.Contains(cell))
					{
						toRemove.Add(diggable);
					}
				}

				// Remove Phantoms
				foreach (var d in toRemove)
				{
					//DebugConsole.Log($"[WorldStateSyncer] Removing phantom dig at {Grid.PosToCell(d)}");
					d.gameObject.DeleteObject();
				}

				// Add Missing
				foreach (var cell in packet.DigCells)
				{
					if (!localDigs.Contains(cell))
					{
						//DebugConsole.Log($"[WorldStateSyncer] Adding missing dig at {cell}");
						// Use DigTool logic without sending a packet back!
						// We can manually instantiate the DigPlacer.
						if (Grid.IsValidCell(cell) && Grid.Solid[cell])
						{
							// DigTool.PlaceDig might trigger patches. 
							// We should instantiate the prefab directly to avoid triggering client->host packets.
							GameObject prefab = Assets.GetPrefab("DigPlacer");
							if (prefab != null)
							{
								Vector3 pos = Grid.CellToPosCBC(cell, Grid.SceneLayer.Move);
								GameObject go = Util.KInstantiate(prefab, pos);
								go.SetActive(true);
							}
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[WorldStateSyncer] Error in OnDiggingStateReceived: {ex.Message}");
			}
		}

		// --- Chore Logic (Mopping) ---

		private void SyncChores()
		{
			var chorePacket = new ChoreStatePacket();

			try
			{
				// Use our tracked mop placers
				lock (MopTracker.MopPlacers)
				{
					foreach (var go in MopTracker.MopPlacers)
					{
						if (go == null) continue;
						int cell = Grid.PosToCell(go);
						chorePacket.Chores.Add(new ChoreData { Cell = cell, Type = SyncedChoreType.Mop });
					}
				}

				PacketSender.SendToAllClients(chorePacket, SteamNetworkingSend.Unreliable);
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[WorldStateSyncer] Error in SyncChores: {ex}");
			}
		}

		public void OnChoreStateReceived(ChoreStatePacket packet)
		{
			try
			{
				// Reconcile Mops
				var localMops = new HashSet<int>();
				var toRemove = new List<GameObject>();

				lock (MopTracker.MopPlacers)
				{
					// Identification Phase
					foreach (var go in MopTracker.MopPlacers)
					{
						if (go == null) continue;
						int cell = Grid.PosToCell(go);
						localMops.Add(cell);

						// Check if phantom
						bool existsRemote = false;
						foreach (var c in packet.Chores)
						{
							if (c.Cell == cell && c.Type == SyncedChoreType.Mop)
							{
								existsRemote = true;
								break;
							}
						}

						if (!existsRemote)
						{
							toRemove.Add(go);
						}
					}
				}

				// Removal Phase
				foreach (var go in toRemove)
				{
					go.DeleteObject();
					// MopTracker will update via OnCleanUp patch automatically
				}

				// Addition Phase
				foreach (var c in packet.Chores)
				{
					if (c.Type == SyncedChoreType.Mop && !localMops.Contains(c.Cell))
					{
						// Spawn Mop Placer
						if (Grid.IsValidCell(c.Cell))
						{
							var mopPrefab = Assets.GetPrefab(new Tag("MopPlacer"));
							if (mopPrefab != null)
							{
								GameObject placer = Util.KInstantiate(mopPrefab);
								Vector3 position = Grid.CellToPosCBC(c.Cell, MopTool.Instance.visualizerLayer);
								position.z -= 0.15f;
								placer.transform.SetPosition(position);
								placer.SetActive(true);

								// Set standard priority if possible (default 5)
								var prioritizable = placer.GetComponent<Prioritizable>();
								if (prioritizable != null && ToolMenu.Instance != null)
									prioritizable.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());
							}
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[WorldStateSyncer] Error in OnChoreStateReceived: {ex.Message}");
			}
		}

		// --- Research Logic ---
		private void SyncResearch()
		{
			if (Db.Get().Techs == null || Research.Instance == null) return;

			try
			{
				var packet = new ResearchStatePacket();

				// Include the current active research
				var activeResearch = Research.Instance.GetActiveResearch();
				packet.ActiveTechId = activeResearch?.tech?.Id ?? string.Empty;

				// Include the research queue
				try
				{
					var queueField = HarmonyLib.AccessTools.Field(typeof(Research), "queuedTech");
					if (queueField != null)
					{
						var queue = queueField.GetValue(Research.Instance) as System.Collections.IList;
						if (queue != null)
						{
							foreach (var item in queue)
							{
								var techInstance = item as TechInstance;
								if (techInstance?.tech != null)
								{
									packet.QueuedTechIds.Add(techInstance.tech.Id);
								}
							}
						}
					}
				}
				catch { }

				if (Db.Get().Techs != null)
				{
					foreach (var tech in Db.Get().Techs.resources)
					{
						var techInst = Research.Instance.Get(tech);
						if (techInst != null && techInst.IsComplete())
						{
							packet.UnlockedTechIds.Add(tech.Id);
						}
					}
				}

				PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[WorldStateSyncer] Error in SyncResearch: {ex.Message}");
			}
		}

		// --- Research Progress Logic ---
		private void SyncResearchProgress()
		{
			if (Research.Instance == null) return;

			try
			{
				var activeResearch = Research.Instance.GetActiveResearch();
				if (activeResearch == null || activeResearch.tech == null) return;

				var techInstance = activeResearch;
				var tech = techInstance.tech;
				
				// Calculate total progress percentage
				float totalCost = 0f;
				float totalProgress = 0f;
				
				foreach (var researchType in tech.costsByResearchTypeID.Keys)
				{
					float cost = tech.costsByResearchTypeID[researchType];
					float points = techInstance.progressInventory.PointsByTypeID.ContainsKey(researchType) 
						? techInstance.progressInventory.PointsByTypeID[researchType] 
						: 0f;
					
					totalCost += cost;
					totalProgress += Mathf.Min(points, cost);
				}
				
				float progressPercent = totalCost > 0 ? totalProgress / totalCost : 0f;
				
				var packet = new ResearchProgressPacket
				{
					TechId = tech.Id,
					Progress = progressPercent
				};
				
				PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[WorldStateSyncer] Error in SyncResearchProgress: {ex.Message}");
			}
		}

		// --- Priorities Logic ---
		private void SyncPriorities()
		{
			try
			{
				var packet = new PrioritizeStatePacket();

				foreach (var identity in NetworkIdentityRegistry.AllIdentities)
				{
					if (identity == null) continue;

					var prioritizable = identity.GetComponent<Prioritizable>();
					if (prioritizable != null && prioritizable.IsPrioritizable())
					{
						var output = prioritizable.GetMasterPriority();

						packet.Priorities.Add(new PrioritizeStatePacket.PriorityData
						{
							NetId = identity.NetId,
							PriorityClass = (int)output.priority_class,
							PriorityValue = output.priority_value
						});
					}
				}

				if (packet.Priorities.Count > 0)
					PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[WorldStateSyncer] Error in SyncPriorities: {ex.Message}");
			}
		}

		private System.Reflection.FieldInfo _disinfectChoreField;

		private void SyncDisinfectImpl()
		{
			try
			{
				// Use our tracker
				lock (DisinfectTracker.Disinfectables)
				{
					if (DisinfectTracker.Disinfectables.Count == 0) return;

					if (_disinfectChoreField == null)
					{
						_disinfectChoreField = typeof(Disinfectable).GetField("chore", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
					}

					var packet = new DisinfectStatePacket();
					foreach (var disinfectable in DisinfectTracker.Disinfectables)
					{
						if (disinfectable == null) continue;

						object chore = _disinfectChoreField?.GetValue(disinfectable);
						if (chore != null)
						{
							int cell = Grid.PosToCell(disinfectable);
							packet.DisinfectCells.Add(cell);
						}
					}

					if (packet.DisinfectCells.Count > 0)
						PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
				}
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[WorldStateSyncer] Error in SyncDisinfectImpl: {ex.Message}");
			}
		}

		public void OnDisinfectStateReceived(DisinfectStatePacket packet)
		{
			try
			{
				lock (DisinfectTracker.Disinfectables)
				{
					if (DisinfectTracker.Disinfectables.Count == 0) return;

					if (_disinfectChoreField == null)
					{
						_disinfectChoreField = typeof(Disinfectable).GetField("chore", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
					}

					foreach (var disinfectable in DisinfectTracker.Disinfectables)
					{
						if (disinfectable == null) continue;
						int cell = Grid.PosToCell(disinfectable);

						object chore = _disinfectChoreField?.GetValue(disinfectable);
						bool isMarked = chore != null;

						if (packet.DisinfectCells.Contains(cell))
						{
							if (!isMarked)
							{
								disinfectable.MarkForDisinfect();
							}
						}
						else
						{
							if (isMarked)
							{
								disinfectable.Trigger((int)GameHashes.Cancel, null);
							}
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[WorldStateSyncer] Error in OnDisinfectStateReceived: {ex.Message}");
			}
		}
		// --- Gas and Liquid Logic ---
		private void SyncGasLiquid()
		{
			if (Grid.WidthInCells == 0 || Grid.HeightInCells == 0) return;

			// Initialize Shadow Grid if needed
			if (_shadowElements == null)
			{
				_shadowElements = new ushort[Grid.CellCount];
				_shadowMass = new float[Grid.CellCount];

				// First run: Copy current state to avoid sending entire map!
				for (int i = 0; i < Grid.CellCount; i++)
				{
					_shadowElements[i] = Grid.ElementIdx[i];
					_shadowMass[i] = Grid.Mass[i];
				}
				return; // Wait for next tick to sync *changes*
			}

			// Iterate over Active Viewports
			// Union of all viewports to avoid duplicate checks? 
			// Or just simple iteration (duplicates are cheap to check against shadow grid).

			// Add local player viewport
			if (CursorManager.Instance != null && Camera.main != null)
			{
				// We don't have a CSteamID for local in the dict usually, or we can just calculate it here.
				// Let's just create a temp rect.
				Camera cam = Camera.main;
				Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
				Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
				Grid.PosToXY(bl, out int x1, out int y1);
				Grid.PosToXY(tr, out int x2, out int y2);

				// Add margin
				int margin = 2;
				x1 = Mathf.Max(0, x1 - margin);
				y1 = Mathf.Max(0, y1 - margin);
				x2 = Mathf.Min(Grid.WidthInCells, x2 + margin);
				y2 = Mathf.Min(Grid.HeightInCells, y2 + margin);

				ScanArea(x1, y1, x2, y2);
			}

			// Scan Client Viewports
			foreach (var kvp in _clientViewports)
			{
				var rect = kvp.Value;
				int x1 = Mathf.Max(0, rect.xMin - 2);
				int y1 = Mathf.Max(0, rect.yMin - 2);
				int x2 = Mathf.Min(Grid.WidthInCells, rect.xMax + 2);
				int y2 = Mathf.Min(Grid.HeightInCells, rect.yMax + 2);

				ScanArea(x1, y1, x2, y2);
			}

			// Flush the batcher
			ONI_MP.Misc.World.WorldUpdateBatcher.Flush();
		}

		private void ScanArea(int x1, int y1, int x2, int y2)
		{
			for (int y = y1; y < y2; y++)
			{
				for (int x = x1; x < x2; x++)
				{
					int cell = y * Grid.WidthInCells + x;
					if (!Grid.IsValidCell(cell)) continue;

					ushort currentElement = Grid.ElementIdx[cell];
					float currentMass = Grid.Mass[cell];

					// Optimization: Ignore very small mass changes?
					// Gas flow changes mass constantly.
					bool changed = false;

					if (currentElement != _shadowElements[cell]) changed = true;
					else if (Mathf.Abs(currentMass - _shadowMass[cell]) > 0.01f) changed = true; // 10g threshold

					if (changed)
					{
						// Update Shadow
						_shadowElements[cell] = currentElement;
						_shadowMass[cell] = currentMass;

						// Queue for Network
						ONI_MP.Misc.World.WorldUpdateBatcher.Queue(new WorldUpdatePacket.CellUpdate
						{
							Cell = cell,
							ElementIdx = currentElement,
							Mass = currentMass,
							Temperature = Grid.Temperature[cell],
							DiseaseIdx = Grid.DiseaseIdx[cell],
							DiseaseCount = Grid.DiseaseCount[cell]
						});
					}
				}
			}
		}
	}
}
