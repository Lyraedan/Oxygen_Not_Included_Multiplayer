using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.World;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shared.Profiling;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
    public class BuildingSyncer : MonoBehaviour
    {
        public static BuildingSyncer Instance { get; private set; }

        private const float SYNC_INTERVAL = 30f;
        private float _lastSyncTime;

        private bool _initialized = false;
        private float _initializationTime;
        private const float INITIAL_DELAY = 5f;

        // Chunk reassembly
        private Dictionary<int, List<BuildingState>> _chunkBuffer = new();
        private int _expectedChunks = -1;
        private float _lastChunkTime;
        private const float CHUNK_TIMEOUT = 5f;

        private void Awake()
        {
            Profiler.Scope();

            Instance = this;
        }

        private void Update()
        {
            Profiler.Scope();

            if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost)
                return;

            if (MultiplayerSession.ConnectedPlayers.Count == 0)
                return;

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

            // Timeout protection (client-side safety)
            if (_chunkBuffer.Count > 0 && Time.time - _lastChunkTime > CHUNK_TIMEOUT)
            {
                DebugConsole.Log("[BuildingSyncer] Chunk timeout - clearing buffer");
                _chunkBuffer.Clear();
                _expectedChunks = -1;
            }
        }

        private void SendSyncPacket()
        {
            Profiler.Scope();

            var buildings = global::Components.BuildingCompletes.Items;
            var stateList = new List<BuildingState>(buildings.Count);

            bool isLan = NetworkConfig.IsLanConfig();
            float maxPacketSize = isLan
                ? PacketSender.MAX_PACKET_SIZE_LAN * 1024f
                : PacketSender.MAX_PACKET_SIZE_UNRELIABLE;

            foreach (var building in buildings)
            {
                if (building == null) continue;

                int cell = Grid.PosToCell(building);
                if (!Grid.IsValidCell(cell)) continue;

                var kpid = building.GetComponent<KPrefabID>();
                if (kpid == null) continue;

                stateList.Add(new BuildingState
                {
                    Cell = cell,
                    PrefabName = kpid.PrefabTag.Name
                });
            }

            // Build chunks
            List<List<BuildingState>> chunks = new();
            List<BuildingState> currentBatch = new();

            foreach (var state in stateList)
            {
                currentBatch.Add(state);

                var testPacket = new BuildingStatePacket
                {
                    Buildings = currentBatch
                };

                int size = testPacket.SerializeToByteArray().Length + 4;

                if (size > maxPacketSize)
                {
                    currentBatch.RemoveAt(currentBatch.Count - 1);

                    chunks.Add(currentBatch);
                    currentBatch = new List<BuildingState> { state };
                }
            }

            if (currentBatch.Count > 0)
                chunks.Add(currentBatch);

            // Send chunks with metadata
            for (int i = 0; i < chunks.Count; i++)
            {
                SendBatch(chunks[i], i, chunks.Count);
            }
        }

        private void SendBatch(List<BuildingState> batch, int chunkIndex, int totalChunks)
        {
            Profiler.Scope();

            var packet = new BuildingStatePacket
            {
                Buildings = batch,
                ChunkIndex = chunkIndex,
                TotalChunks = totalChunks
            };

            PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
        }

        public void OnPacketReceived(BuildingStatePacket packet)
        {
            Profiler.Scope();

            if (MultiplayerSession.IsHost) return;
            if (Grid.WidthInCells == 0) return;

            _chunkBuffer[packet.ChunkIndex] = packet.Buildings;
            _expectedChunks = packet.TotalChunks;
            _lastChunkTime = Time.time;

            if (_chunkBuffer.Count < _expectedChunks)
                return;

            // Reassemble full dataset
            var fullList = new List<BuildingState>();
            for (int i = 0; i < _expectedChunks; i++)
            {
                if (_chunkBuffer.TryGetValue(i, out var chunk))
                    fullList.AddRange(chunk);
            }

            _chunkBuffer.Clear();
            _expectedChunks = -1;

            StartCoroutine(Reconcile(fullList));
        }

        private IEnumerator Reconcile(List<BuildingState> remoteBuildings)
        {
            Profiler.Scope();

            // Build remote lookup: (Cell, Layer) -> Prefab
            var remoteByCellLayer = new Dictionary<(int, ObjectLayer), string>();
            foreach (var b in remoteBuildings)
            {
                if (!string.IsNullOrEmpty(b.PrefabName))
                {
                    var def = Assets.GetBuildingDef(b.PrefabName);
                    if (def != null)
                    {
                        remoteByCellLayer[(b.Cell, def.TileLayer)] = b.PrefabName;
                    }
                }
            }

            var localBuildings = global::Components.BuildingCompletes.Items;
            var localList = new List<BuildingComplete>(localBuildings);

            // Replace buildings ONLY if something different exists at same cell AND same layer
            foreach (var building in localList)
            {
                if (building == null) continue;

                ObjectLayer layer = building.Def.TileLayer;
                int cell = Grid.PosToCell(building);
                var kpid = building.GetComponent<KPrefabID>();
                if (kpid == null) continue;

                string localPrefab = kpid.PrefabTag.Name;

                if (remoteByCellLayer.TryGetValue((cell, layer), out var remotePrefab))
                {
                    if (remotePrefab != localPrefab)
                    {
                        DebugConsole.Log($"[BuildingSyncer] Replacing {localPrefab} with {remotePrefab} at {cell} (Layer: {layer})");
                        Util.KDestroyGameObject(building.gameObject);
                    }
                }
                // If no remote building at this cell+layer then do nothing
            }

            // Build a set of (cell, layer, prefab) for existing local buildings
            var localSet = new HashSet<(int, ObjectLayer, string)>();
            foreach (var building in global::Components.BuildingCompletes.Items)
            {
                if (building == null) continue;

                int cell = Grid.PosToCell(building);
                var kpid = building.GetComponent<KPrefabID>();
                if (kpid == null) continue;

                localSet.Add((cell, building.Def.TileLayer, kpid.PrefabTag.Name));
            }

            // Spawn missing remote buildings
            foreach (var remote in remoteBuildings)
            {
                if (string.IsNullOrEmpty(remote.PrefabName)) continue;

                var def = Assets.GetBuildingDef(remote.PrefabName);
                if (def == null) continue;

                if (!localSet.Contains((remote.Cell, def.TileLayer, remote.PrefabName)))
                {
                    DebugConsole.Log($"[BuildingSyncer] Spawning missing building {remote.PrefabName} at {remote.Cell} (Layer: {def.TileLayer})");
                    SpawnBuilding(remote.Cell, remote.PrefabName);
                    yield return null;
                }
            }
        }

        private void SpawnBuilding(int cell, string prefabName)
        {
            Profiler.Scope();

            if (Grid.WidthInCells == 0) return;
            if (string.IsNullOrEmpty(prefabName)) return;

            var def = Assets.GetBuildingDef(prefabName);

            if (def == null)
            {
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
                    Vector3 pos = Grid.CellToPosCBC(cell, def.SceneLayer);
                    GameObject go = Util.KInstantiate(Assets.GetPrefab(def.Tag), pos);

                    if (go != null)
                    {
                        var primaryElement = go.GetComponent<PrimaryElement>();
                        if (primaryElement != null)
                        {
                            var safeElement = ElementLoader.FindElementByHash(SimHashes.SandStone)
                                ?? ElementLoader.FindElementByHash(SimHashes.Dirt)
                                ?? ElementLoader.elements?.FirstOrDefault(e => e.IsSolid);

                            if (safeElement != null)
                            {
                                primaryElement.SetElement(safeElement.id, true);
                                primaryElement.Temperature = 293.15f;
                                if (primaryElement.Mass <= 0.001f)
                                    primaryElement.Mass = 100f;
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