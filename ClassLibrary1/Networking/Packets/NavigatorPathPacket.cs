﻿using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ONI_MP.DebugTools;
using ONI_MP.Patches.Navigation;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace ONI_MP.Networking.Packets
{
    public class NavigatorPathPacket : IPacket
    {
        public int NetId;

        public struct PathStep
        {
            public int Cell;
            public NavType NavType;
            public byte TransitionId;

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Cell);
                writer.Write((byte)NavType);
                writer.Write(TransitionId);
            }

            public static PathStep Deserialize(BinaryReader reader)
            {
                return new PathStep
                {
                    Cell = reader.ReadInt32(),
                    NavType = (NavType)reader.ReadByte(),
                    TransitionId = reader.ReadByte()
                };
            }
        }

        public List<PathStep> Steps = new List<PathStep>();

        public PacketType Type => PacketType.NavigatorPath;

        public void Serialize(BinaryWriter writer)
        {
            using (var memStream = new MemoryStream())
            {
                using (var tempWriter = new BinaryWriter(memStream, System.Text.Encoding.Default, leaveOpen: true))
                {
                    tempWriter.Write(NetId);
                    tempWriter.Write(Steps.Count);
                    foreach (var step in Steps)
                        step.Serialize(tempWriter);
                }

                byte[] rawData = memStream.ToArray();

                using (var compressed = new MemoryStream())
                {
                    using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
                    {
                        gzip.Write(rawData, 0, rawData.Length);
                    }

                    byte[] compressedBytes = compressed.ToArray();
                    writer.Write(compressedBytes.Length);
                    writer.Write(compressedBytes);
                }
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            int compressedLength = reader.ReadInt32();
            byte[] compressedBytes = reader.ReadBytes(compressedLength);

            using (var compressed = new MemoryStream(compressedBytes))
            using (var gzip = new GZipStream(compressed, CompressionMode.Decompress))
            using (var decompressed = new MemoryStream())
            {
                gzip.CopyTo(decompressed);
                decompressed.Position = 0;

                using (var tempReader = new BinaryReader(decompressed))
                {
                    NetId = tempReader.ReadInt32();
                    int count = tempReader.ReadInt32();

                    Steps.Clear();
                    for (int i = 0; i < count; i++)
                        Steps.Add(PathStep.Deserialize(tempReader));
                }
            }
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost)
                return;

            if (!NetEntityRegistry.TryGet(NetId, out var entity))
            {
                DebugConsole.LogWarning($"[NavigatorPathPacket] Could not find entity with NetId {NetId}");
                return;
            }

            if(!entity)
                return;

            if (!entity.TryGetComponent(out Navigator navigator))
            {
                DebugConsole.LogWarning($"[NavigatorPathPacket] Entity {NetId} has no Navigator");
                return;
            }

            if (!navigator)
                return;

            if (Steps.Count < 2)
            {
                DebugConsole.LogWarning($"[NavigatorPathPacket] Received invalid path for {NetId}");
                return;
            }

            var newPath = new PathFinder.Path
            {
                nodes = new List<PathFinder.Path.Node>(Steps.Count)
            };

            foreach (var step in Steps)
            {
                newPath.nodes.Add(new PathFinder.Path.Node
                {
                    cell = step.Cell,
                    navType = step.NavType,
                    transitionId = step.TransitionId
                });
            }

            navigator.path = newPath;
            DebugConsole.Log($"Got path: {newPath}");
            return;

            // Final destination position
            int finalCell = Steps[Steps.Count - 1].Cell;
            Vector3 finalPos = Grid.CellToPosCBC(finalCell, Grid.SceneLayer.Move);

            // Create a dummy GameObject
            GameObject dummyTarget = new GameObject($"NetNav_Target_{NetId}");
            dummyTarget.transform.position = finalPos;
            dummyTarget.transform.SetParent(Game.Instance.transform, worldPositionStays: true);

            var targetBehaviour = dummyTarget.AddComponent<KMonoBehaviour>();

            System.Action cleanup = () =>
            {
                if (dummyTarget != null)
                {
                    UnityEngine.Object.Destroy(dummyTarget);
                    DebugConsole.Log($"[NavigatorPathPacket] Cleaned up dummy target for NetId {NetId}");
                }
            };

            // Inject callback into navigator events
            navigator.Subscribe((int)GameHashes.DestinationReached, (data) => cleanup.Invoke());
            navigator.Subscribe((int)GameHashes.NavigationFailed, (data) => cleanup.Invoke());

            // Trigger movement
            bool result = navigator.ClientGoTo(targetBehaviour, new CellOffset[] { CellOffset.none });

            if (!result)
            {
                DebugConsole.LogWarning($"[NavigatorPathPacket] ClientGoTo failed for {NetId}");
                UnityEngine.Object.Destroy(dummyTarget); // immediate fallback cleanup
            }

            DebugConsole.Log($"[NavigatorPathPacket] Path with {Steps.Count} nodes applied to NetId {NetId}");
        }
    }
}
