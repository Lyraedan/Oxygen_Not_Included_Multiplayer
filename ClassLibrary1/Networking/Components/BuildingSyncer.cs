using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.World;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class BuildingSyncer : MonoBehaviour
	{
		public static BuildingSyncer Instance { get; private set; }

		private const float SYNC_INTERVAL = 30f; // Increased from 10s, helps sandbox mode
		private float _lastSyncTime;

		// Grace period
		private bool _initialized = false;
		private float _initializationTime;
		private const float INITIAL_DELAY = 5f;

		private void Awake()
		{
			Instance = this;
		}

		private void Update()
		{
			if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost)
				return;

			// Skip if no clients connected
			if (MultiplayerSession.ConnectedPlayers.Count == 0)
				return;

			// Grace period after world load
			if (!_initialized)
			{
				_initializationTime = Time.unscaledTime;
				_initialized = true;
				return;
			}

			if (Time.unscaledTime - _initializationTime < INITIAL_DELAY)
				return;

			if (Time.unscaledTime - _lastSyncTime > SYNC_INTERVAL)
			{
				_lastSyncTime = Time.unscaledTime;
				SendSyncPacket();
			}
		}

		private void SendSyncPacket()
		{
			var buildings = global::Components.BuildingCompletes.Items;
			var stateList = new List<BuildingState>(buildings.Count);

			foreach (var building in buildings)
			{
				if (building == null) continue;

				int cell = Grid.PosToCell(building);
				if (!Grid.IsValidCell(cell)) continue;

				// Use KPrefabID for identification
				var kpid = building.GetComponent<KPrefabID>();
				if (kpid == null) continue;

				stateList.Add(new BuildingState
				{
					Cell = cell,
					PrefabName = kpid.PrefabTag.Name  // Send string name instead of hash
				});
			}

			var packet = new BuildingStatePacket
			{
				Buildings = stateList
			};

			PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
			//DebugConsole.Log($"[BuildingSyncer] Sent sync packet with {stateList.Count} buildings.");
		}

		public void OnPacketReceived(BuildingStatePacket packet)
		{
			if (MultiplayerSession.IsHost) return;
			if (Grid.WidthInCells == 0) return; // World not loaded yet

			// DebugConsole.Log($"[BuildingSyncer] Received sync packet with {packet.Buildings.Count} remote buildings.");
			StartCoroutine(Reconcile(packet.Buildings));
		}

		private IEnumerator Reconcile(List<BuildingState> remoteBuildings)
		{
			// Create a lookup for remote buildings: Cell -> List of PrefabNames
			var remoteSet = new HashSet<(int, string)>();
			foreach (var b in remoteBuildings)
			{
				if (!string.IsNullOrEmpty(b.PrefabName))
				{
					remoteSet.Add((b.Cell, b.PrefabName));
				}
			}

			var localBuildings = global::Components.BuildingCompletes.Items;
			// We need a stable list copy since we might modify it
			var localList = new List<BuildingComplete>(localBuildings);

			// 1. Destroy Phantom Buildings (Local but not Remote)
			foreach (var building in localList)
			{
				if (building == null) continue;

				int cell = Grid.PosToCell(building);
				var kpid = building.GetComponent<KPrefabID>();
				if (kpid == null) continue;

				string prefabName = kpid.PrefabTag.Name;

				if (!remoteSet.Contains((cell, prefabName)))
				{
					DebugConsole.Log($"[BuildingSyncer] Removing phantom building {prefabName} at {cell}");
					// Use standard deconstruction or immediate destroy? Immediate is safer for sync fix
					Util.KDestroyGameObject(building.gameObject);
				}
			}

			// 2. Spawn Missing Buildings (Remote but not Local)
			// This is O(N*M) if naive. Let's build a local set first.
			var localSet = new HashSet<(int, string)>();
			foreach (var building in localList)
			{
				if (building == null) continue;
				int cell = Grid.PosToCell(building);
				var kpid = building.GetComponent<KPrefabID>();
				if (kpid == null) continue;
				localSet.Add((cell, kpid.PrefabTag.Name));
			}

			foreach (var remote in remoteBuildings)
			{
				if (string.IsNullOrEmpty(remote.PrefabName)) continue;
				
				if (!localSet.Contains((remote.Cell, remote.PrefabName)))
				{
					DebugConsole.Log($"[BuildingSyncer] Spawning missing building {remote.PrefabName} at {remote.Cell}");
					SpawnBuilding(remote.Cell, remote.PrefabName);
					yield return null; // Spread out instantiation to avoid frame spikes
				}
			}
		}

		private void SpawnBuilding(int cell, string prefabName)
		{
			if (Grid.WidthInCells == 0) return;
			if (string.IsNullOrEmpty(prefabName)) return;

			var def = Assets.GetBuildingDef(prefabName);

			if (def == null)
			{
				// Try fallback via prefab lookup
				GameObject prefab = Assets.GetPrefab(prefabName);
				if (prefab != null)
				{
					var wBuilding = prefab.GetComponent<Building>();
					if (wBuilding != null) def = wBuilding.Def;
				}
			}

			if (def != null)
			{
				try
				{
					// Use Util.KInstantiate to bypass complex BuildingDef.Build logic that causes DivideByZero
					// This is a "visual/functional" spawn for client sync.
					Vector3 pos = Grid.CellToPosCBC(cell, def.SceneLayer);
					GameObject go = Util.KInstantiate(Assets.GetPrefab(def.Tag), pos);

					if (go != null)
					{
						// Try to set element to something safe so it has properties (mass, temp, etc)
						var primaryElement = go.GetComponent<PrimaryElement>();
						if (primaryElement != null)
						{
							// "Bloco" (Tile) needs a solid element.
							var safeElement = ElementLoader.FindElementByHash(SimHashes.SandStone);
							if (safeElement == null) safeElement = ElementLoader.FindElementByHash(SimHashes.Dirt);
							if (safeElement == null && ElementLoader.elements != null) safeElement = ElementLoader.elements.FirstOrDefault(e => e.IsSolid);

							if (safeElement != null)
							{
								primaryElement.SetElement(safeElement.id, true);
								primaryElement.Temperature = 293.15f;
								if (primaryElement.Mass <= 0.001f) primaryElement.Mass = 100f; // Ensure non-zero mass
							}
						}

						go.SetActive(true);
					}
				}
				catch (System.Exception ex)
				{
					DebugConsole.LogError($"[BuildingSyncer] Failed to spawn building {def.Name} at {cell}: {ex}");
				}
			}
			else
			{
				DebugConsole.LogWarning($"[BuildingSyncer] Could not find BuildingDef for {prefabName}");
			}
		}
	}
}
