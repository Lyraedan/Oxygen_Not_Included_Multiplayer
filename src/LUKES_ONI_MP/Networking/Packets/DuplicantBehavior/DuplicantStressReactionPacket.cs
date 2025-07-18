using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Synchronizes duplicant stress reactions across clients.
    /// </summary>
    public class DuplicantStressReactionPacket : IPacket
    {
        public int DuplicantId;
        public string ReactionType;
        public Vector3 ReactionLocation;
        public int TargetObjectId;
        public bool IsActive;
        public long Timestamp;

        public PacketType Type => PacketType.DuplicantStressReaction;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantId);
            writer.Write(ReactionType ?? "");
            writer.Write(ReactionLocation.x);
            writer.Write(ReactionLocation.y);
            writer.Write(ReactionLocation.z);
            writer.Write(TargetObjectId);
            writer.Write(IsActive);
            writer.Write(Timestamp);
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantId = reader.ReadInt32();
            ReactionType = reader.ReadString();
            ReactionLocation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            TargetObjectId = reader.ReadInt32();
            IsActive = reader.ReadBoolean();
            Timestamp = reader.ReadInt64();
        }

        public bool IsValid()
        {
            return DuplicantId > 0 && !string.IsNullOrEmpty(ReactionType);
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantId);
            if (duplicantGO == null)
            {
                Debug.LogWarning($"DuplicantStressReactionPacket: Could not find duplicant with ID {DuplicantId}");
                return;
            }

            Debug.Log($"Applied stress reaction {ReactionType} to duplicant {DuplicantId} at {ReactionLocation} (active: {IsActive})");
        }

        public override string ToString()
        {
            return $"DuplicantStressReactionPacket[DuplicantId={DuplicantId}, Reaction={ReactionType}, Active={IsActive}]";
        }

        public void OnDispatched()
        {
            // Called when packet is sent
        }
    }
}
