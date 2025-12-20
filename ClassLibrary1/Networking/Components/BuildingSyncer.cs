using ONI_MP.Api.Networking;
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

		private const float SYNC_INTERVAL = 10f; // Sync every 10 seconds
		private float _lastSyncTime;

		private void Awake()
		{
			Instance = this;
		}

		private void Update()
		{
			if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost)
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
					PrefabHash = kpid.PrefabTag.GetHash()
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
			// Create a lookup for remote buildings: Cell -> List of PrefabHashes (in case multiple buildings on one cell, e.g. pipes)
			// Using a dictionary of lists to handle multiple objects per cell if necessary, though mainly buildings are layer-separated.
			// For simplicity, let's just use a HashSet of unique identifiers (Cell + Hash)
			var remoteSet = new HashSet<(int, int)>();
			foreach (var b in remoteBuildings)
			{
				remoteSet.Add((b.Cell, b.PrefabHash));
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

				int hash = kpid.PrefabTag.GetHash();

				if (!remoteSet.Contains((cell, hash)))
				{
					DebugConsole.Log($"[BuildingSyncer] Removing phantom building {kpid.PrefabTag.Name} at {cell}");
					// Use standard deconstruction or immediate destroy? Immediate is safer for sync fix
					Util.KDestroyGameObject(building.gameObject);
				}
			}

			// 2. Spawn Missing Buildings (Remote but not Local)
			// This is O(N*M) if naive. Let's build a local set first.
			var localSet = new HashSet<(int, int)>();
			foreach (var building in localList)
			{
				if (building == null) continue;
				int cell = Grid.PosToCell(building);
				var kpid = building.GetComponent<KPrefabID>();
				if (kpid == null) continue;
				localSet.Add((cell, kpid.PrefabTag.GetHash()));
			}

			foreach (var remote in remoteBuildings)
			{
				if (!localSet.Contains((remote.Cell, remote.PrefabHash)))
				{
					DebugConsole.Log($"[BuildingSyncer] Spawning missing building Hash:{remote.PrefabHash} at {remote.Cell}");
					SpawnBuilding(remote.Cell, remote.PrefabHash);
					yield return null; // Spread out instantiation to avoid frame spikes
				}
			}
		}

		private void SpawnBuilding(int cell, int prefabHash)
		{
			if (Grid.WidthInCells == 0) return;

			Tag prefabTag = new Tag(prefabHash);
			var def = Assets.GetBuildingDef(prefabTag.Name);

			if (def == null)
			{
				// Try to find by hash if name lookup fails, but BuildingDef usually needs string ID.
				// Assets.GetBuildingDef expects a string ID.
				// We need to map Hash -> String. KPrefabID doesn't easily store the reverse map globally potentially.
				// However, Assets.BuildingDefs is a list. We might need to search it?
				// optimization: The PrefabTag.Name IS the ID string usually. 
				// Wait, Tag.GetHash() is just the integer. We need the string to lookup the Def.
				// TagManager might help, or we assume we can get the string from the Tag if it's in the system.
				// Actually Tag structure has 'Name' property if it was created from string.
				// But we deserialized an int. We need to look up the Tag name from the Hash.
				// ONI has `Assets.GetPrefab(Tag)`

				GameObject prefab = Assets.GetPrefab(prefabTag);
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
				// DebugConsole.LogWarning($"[BuildingSyncer] Could not find BuildingDef for hash {prefabHash}");
			}
		}
	}
}
