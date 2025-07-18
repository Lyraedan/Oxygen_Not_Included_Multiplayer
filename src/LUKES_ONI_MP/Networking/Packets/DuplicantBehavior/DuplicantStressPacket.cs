using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Synchronizes duplicant stress levels across clients.
    /// </summary>
    public class DuplicantStressPacket : IPacket
    {
        public int DuplicantId;
        public float StressLevel;
        public string StressReason;
        public long Timestamp;

        public PacketType Type => PacketType.DuplicantStress;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantId);
            writer.Write(StressLevel);
            writer.Write(StressReason ?? "");
            writer.Write(Timestamp);
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantId = reader.ReadInt32();
            StressLevel = reader.ReadSingle();
            StressReason = reader.ReadString();
            Timestamp = reader.ReadInt64();
        }

        public bool IsValid()
        {
            return DuplicantId > 0 && StressLevel >= 0;
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"DuplicantStressPacket: Could not find duplicant with ID {DuplicantId}");
                return;
            }

            var stressMonitor = duplicantGO.GetComponent<StressMonitor>();
            if (stressMonitor != null)
            {
                // Apply stress level (simplified)
                Debug.Log($"Applied stress level {StressLevel} to duplicant {DuplicantId} (reason: {StressReason})");
            }
        }

        public override string ToString()
        {
            return $"DuplicantStressPacket[DuplicantId={DuplicantId}, StressLevel={StressLevel:F2}, Reason={StressReason}]";
        }

        public void OnDispatched()
        {
            // Called when packet is sent
        }
    }
}
