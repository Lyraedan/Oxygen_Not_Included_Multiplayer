﻿using System;
using System.IO;
using UnityEngine;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Misc;

namespace ONI_MP.Networking.Packets.Events
{
    public class EventTriggeredPacket : IPacket
    {
        public int NetId;
        public int EventHash;
        public string SerializedData;
        public string DataType;

        public PacketType Type => PacketType.EventTriggered;

        public EventTriggeredPacket() { }

        public EventTriggeredPacket(int netId, int eventHash, object data = null)
        {
            NetId = netId;
            EventHash = eventHash;

            if (data != null)
            {
                SerializedData = SafeSerializer.ToJson(data);
                DataType = data.GetType().AssemblyQualifiedName;
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetId);
            writer.Write(EventHash);
            writer.Write(SerializedData ?? string.Empty);
            writer.Write(DataType ?? string.Empty);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetId = reader.ReadInt32();
            EventHash = reader.ReadInt32();
            SerializedData = reader.ReadString();
            DataType = reader.ReadString();
        }

        public void OnDispatched()
        {
            if (!NetworkIdentityRegistry.TryGet(NetId, out var go))
            {
                DebugConsole.LogWarning($"[Packets] Could not find entity with NetId {NetId} for event {EventHash}");
                return;
            }

            object deserialized = null;

            if (!string.IsNullOrEmpty(SerializedData) && !string.IsNullOrEmpty(DataType))
            {
                var type = System.Type.GetType(DataType);
                if (type != null)
                {
                    deserialized = SafeSerializer.FromJson(SerializedData, type);
                }
                else
                {
                    DebugConsole.LogWarning($"[Packets] Failed to resolve type '{DataType}' for event {EventHash}");
                }
            }

            go.Trigger(EventHash, deserialized);
        }
    }
}
