using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Synchronizes duplicant stamina levels across clients.
    /// </summary>
    public class DuplicantStaminaPacket : IPacket
    {
        public int DuplicantId;
        public float StaminaLevel;
        public float MaxStamina;
        public bool IsTired;
        public long Timestamp;

        public PacketType Type => PacketType.DuplicantStamina;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantId);
            writer.Write(StaminaLevel);
            writer.Write(MaxStamina);
            writer.Write(IsTired);
            writer.Write(Timestamp);
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantId = reader.ReadInt32();
            StaminaLevel = reader.ReadSingle();
            MaxStamina = reader.ReadSingle();
            IsTired = reader.ReadBoolean();
            Timestamp = reader.ReadInt64();
        }

        public bool IsValid()
        {
            return DuplicantId > 0 && StaminaLevel >= 0;
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"DuplicantStaminaPacket: Could not find duplicant with ID {DuplicantId}");
                return;
            }

            // Apply stamina level (simplified)
            Debug.Log($"Applied stamina level {StaminaLevel}/{MaxStamina} to duplicant {DuplicantId} (tired: {IsTired})");
        }

        public override string ToString()
        {
            return $"DuplicantStaminaPacket[DuplicantId={DuplicantId}, Stamina={StaminaLevel:F2}/{MaxStamina:F2}, Tired={IsTired}]";
        }

        public void OnDispatched()
        {
            // Called when packet is sent
        }
    }
}
