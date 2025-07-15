using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
    public class DuplicantConditionPacket : IPacket
    {
        public PacketType Type => PacketType.DuplicantCondition;

        public int NetId;
        public float Health;
        public float MaxHealth;
        public float Calories;
        public float Stress;
        public float Breath;
        public float Bladder;
        public float Stamina;
        public float BodyTemperature;
        public float Morale;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetId);
            writer.Write(Health);
            writer.Write(MaxHealth);
            writer.Write(Calories);
            writer.Write(Stress);
            writer.Write(Breath);
            writer.Write(Bladder);
            writer.Write(Stamina);
            writer.Write(BodyTemperature);
            writer.Write(Morale);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetId = reader.ReadInt32();
            Health = reader.ReadSingle();
            MaxHealth = reader.ReadSingle();
            Calories = reader.ReadSingle();
            Stress = reader.ReadSingle();
            Breath = reader.ReadSingle();
            Bladder = reader.ReadSingle();
            Stamina = reader.ReadSingle();
            BodyTemperature = reader.ReadSingle();
            Morale = reader.ReadSingle();
        }

        public void OnDispatched()
        {
            // Disabled for now
            return;

            if (MultiplayerSession.IsHost)
                return;

            if (!NetworkIdentityRegistry.TryGet(NetId, out var targetObject))
            {
                DebugConsole.LogWarning($"[DuplicantConditionPacket] NetId {NetId} not found in registry.");
                return;
            }

            var gameObject = targetObject.gameObject;
            if (gameObject == null)
            {
                DebugConsole.LogWarning($"[DuplicantConditionPacket] NetId {NetId} resolved to non-GameObject.");
                return;
            }

            var tracker = gameObject.GetComponent<ConditionTracker>();
            if (tracker == null)
            {
                DebugConsole.LogWarning($"[DuplicantConditionPacket] GameObject '{gameObject.name}' missing ConditionTracker.");
                return;
            }

            tracker.ApplyHealth(Health, MaxHealth);
            tracker.ApplyAmounts(Calories, Stress, Breath, Bladder, Stamina, BodyTemperature);
            tracker.ApplyAttributes(Morale);
        }
    }
}
