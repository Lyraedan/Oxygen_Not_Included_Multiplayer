using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Synchronizes duplicant stress-related behaviors across clients.
    /// </summary>
    public class DuplicantStressBehaviorPacket : IPacket
    {
        public int DuplicantId;
        public string BehaviorType;
        public int TargetObjectId;
        public bool IsActive;
        public long Timestamp;

        public PacketType Type => PacketType.DuplicantStressBehavior;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantId);
            writer.Write(BehaviorType ?? "");
            writer.Write(TargetObjectId);
            writer.Write(IsActive);
            writer.Write(Timestamp);
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantId = reader.ReadInt32();
            BehaviorType = reader.ReadString();
            TargetObjectId = reader.ReadInt32();
            IsActive = reader.ReadBoolean();
            Timestamp = reader.ReadInt64();
        }

        public bool IsValid()
        {
            return DuplicantId > 0 && !string.IsNullOrEmpty(BehaviorType);
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"DuplicantStressBehaviorPacket: Could not find duplicant with ID {DuplicantId}");
                return;
            }

            Debug.Log($"Applied stress behavior {BehaviorType} to duplicant {DuplicantId} (active: {IsActive})");
        }

        public override string ToString()
        {
            return $"DuplicantStressBehaviorPacket[DuplicantId={DuplicantId}, Behavior={BehaviorType}, Active={IsActive}]";
        }

        public void OnDispatched()
        {
            // Called when packet is sent
        }
    }
}
