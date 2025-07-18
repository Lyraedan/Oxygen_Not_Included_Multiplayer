using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Synchronizes building damage caused by stressed duplicants across clients.
    /// </summary>
    public class BuildingDamageFromStressPacket : IPacket
    {
        public int DuplicantId;
        public int BuildingId;
        public float DamageAmount;
        public Vector3 DamageLocation;
        public string DamageType;
        public long Timestamp;

        public PacketType Type => PacketType.BuildingDamageFromStress;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantId);
            writer.Write(BuildingId);
            writer.Write(DamageAmount);
            writer.Write(DamageLocation.x);
            writer.Write(DamageLocation.y);
            writer.Write(DamageLocation.z);
            writer.Write(DamageType ?? "");
            writer.Write(Timestamp);
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantId = reader.ReadInt32();
            BuildingId = reader.ReadInt32();
            DamageAmount = reader.ReadSingle();
            DamageLocation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            DamageType = reader.ReadString();
            Timestamp = reader.ReadInt64();
        }

        public bool IsValid()
        {
            return DuplicantId > 0 && BuildingId > 0 && DamageAmount > 0;
        }

        public void Execute()
        {
            var duplicantGO = NetworkIdentityRegistry.GetGameObject(DuplicantId);
            var buildingGO = NetworkIdentityRegistry.GetGameObject(BuildingId);
            
            if (duplicantGO == null)
            {
                Debug.LogWarning($"BuildingDamageFromStressPacket: Could not find duplicant with ID {DuplicantId}");
                return;
            }

            if (buildingGO == null)
            {
                Debug.LogWarning($"BuildingDamageFromStressPacket: Could not find building with ID {BuildingId}");
                return;
            }

            Debug.Log($"Applied stress damage {DamageAmount} from duplicant {DuplicantId} to building {BuildingId} at {DamageLocation}");
        }

        public override string ToString()
        {
            return $"BuildingDamageFromStressPacket[DuplicantId={DuplicantId}, BuildingId={BuildingId}, Damage={DamageAmount:F2}]";
        }

        public void OnDispatched()
        {
            // Called when packet is sent
        }
    }
}
