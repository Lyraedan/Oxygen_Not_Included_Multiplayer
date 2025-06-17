﻿using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets
{
    public class InstantiationsPacket : IPacket
    {
        public PacketType Type => PacketType.Instantiations;
        public List<InstantiationEntry> Entries = new List<InstantiationEntry>();

        public struct InstantiationEntry
        {
            public string PrefabName;
            public Vector3 Position;
            public Quaternion Rotation;
            public string ObjectName;
            public bool InitializeId;
            public int GameLayer;
        }

        public void Serialize(BinaryWriter w)
        {
            using (var ms = new MemoryStream())
            {
                using (var deflate = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
                {
                    using (var tempWriter = new BinaryWriter(deflate))
                    {
                        tempWriter.Write(Entries.Count);
                        foreach (var e in Entries)
                        {
                            tempWriter.Write(e.PrefabName ?? "");
                            tempWriter.Write(e.Position.x); tempWriter.Write(e.Position.y); tempWriter.Write(e.Position.z);
                            tempWriter.Write(e.Rotation.x); tempWriter.Write(e.Rotation.y); tempWriter.Write(e.Rotation.z); tempWriter.Write(e.Rotation.w);
                            tempWriter.Write(e.ObjectName ?? "");
                            tempWriter.Write(e.InitializeId);
                            tempWriter.Write(e.GameLayer);
                        }
                    }
                }

                byte[] compressed = ms.ToArray();
                w.Write(compressed.Length);
                w.Write(compressed);
            }
        }

        public void Deserialize(BinaryReader r)
        {
            int compressedLength = r.ReadInt32();
            byte[] compressedData = r.ReadBytes(compressedLength);

            using (var ms = new MemoryStream(compressedData))
            {
                using (var deflate = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    using (var tempReader = new BinaryReader(deflate))
                    {
                        int count = tempReader.ReadInt32();
                        Entries = new List<InstantiationEntry>(count);

                        for (int i = 0; i < count; i++)
                        {
                            Entries.Add(new InstantiationEntry
                            {
                                PrefabName = tempReader.ReadString(),
                                Position = new Vector3(tempReader.ReadSingle(), tempReader.ReadSingle(), tempReader.ReadSingle()),
                                Rotation = new Quaternion(tempReader.ReadSingle(), tempReader.ReadSingle(), tempReader.ReadSingle(), tempReader.ReadSingle()),
                                ObjectName = tempReader.ReadString(),
                                InitializeId = tempReader.ReadBoolean(),
                                GameLayer = tempReader.ReadInt32()
                            });
                        }
                    }
                }
            }
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost) return;

            foreach (var e in Entries)
                Instantiate(e);
        }

        private void Instantiate(InstantiationEntry e)
        {
            GameObject prefab = Assets.GetPrefab(e.PrefabName);
            if (prefab == null)
            {
                DebugConsole.LogWarning($"[InstantiationsPacket] Missing prefab '{e.PrefabName}'");
                return;
            }

            GameObject obj = Object.Instantiate(prefab, e.Position, e.Rotation);
            if (obj == null)
            {
                DebugConsole.LogWarning($"[InstantiationsPacket] Failed to instantiate prefab '{e.PrefabName}'");
                return;
            }

            if (e.GameLayer != 0)
                obj.SetLayerRecursively(e.GameLayer);

            obj.name = e.ObjectName ?? prefab.name;

            KPrefabID id = obj.GetComponent<KPrefabID>();
            if (id != null)
            {
                if (e.InitializeId)
                {
                    id.InstanceID = KPrefabID.GetUniqueID();
                    KPrefabIDTracker.Get().Register(id);
                }

                id.InitializeTags(force_initialize: true);

                KPrefabID source = prefab.GetComponent<KPrefabID>();
                if (source != null)
                {
                    id.CopyTags(source);
                    id.CopyInitFunctions(source);
                }

                id.RunInstantiateFn();
            }

            obj.SetActive(true);
        }
    }
}
