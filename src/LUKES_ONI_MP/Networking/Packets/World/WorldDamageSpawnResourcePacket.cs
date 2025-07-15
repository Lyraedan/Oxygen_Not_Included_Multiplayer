using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.World
{
    public class WorldDamageSpawnResourcePacket : IPacket
    {
        public PacketType Type => PacketType.WorldDamageSpawnResource;

        public int NetId;
        public Vector3 Position;
        public float Mass;
        public float Temperature;
        public ushort ElementIndex;
        public byte DiseaseIndex;
        public int DiseaseCount;

        public WorldDamageSpawnResourcePacket() { }

        public WorldDamageSpawnResourcePacket(int netId, Vector3 pos, float mass, float temp, ushort elementIdx, byte diseaseIdx, int diseaseCount)
        {
            NetId = netId;
            Position = pos;
            Mass = mass;
            Temperature = temp;
            ElementIndex = elementIdx;
            DiseaseIndex = diseaseIdx;
            DiseaseCount = diseaseCount;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetId);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            writer.Write(Mass);
            writer.Write(Temperature);
            writer.Write(ElementIndex);
            writer.Write(DiseaseIndex);
            writer.Write(DiseaseCount);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetId = reader.ReadInt32();
            Position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            Mass = reader.ReadSingle();
            Temperature = reader.ReadSingle();
            ElementIndex = reader.ReadUInt16();
            DiseaseIndex = reader.ReadByte();
            DiseaseCount = reader.ReadInt32();
        }

        public void OnDispatched()
        {
            Element element = ElementLoader.elements[ElementIndex];

            InvokePlaySoundForSubstance(element, Position);

            float dropMass = Mass;
            if (dropMass <= 0f)
                return;

            GameObject dropped = element.substance.SpawnResource(Position, dropMass, Temperature, DiseaseIndex, DiseaseCount);
            NetworkIdentity identity = dropped.GetComponent<NetworkIdentity>();
            identity.OverrideNetId(NetId);
            DebugConsole.Log("[WorldDamageSpawnResourcePacket] Synchronized Network ID");

            Pickupable pickup = dropped.GetComponent<Pickupable>();
            if (pickup != null && pickup.GetMyWorld()?.worldInventory.IsReachable(pickup) == true)
            {
                PopFXManager.Instance.SpawnFX(
                    PopFXManager.Instance.sprite_Resource,
                    Mathf.RoundToInt(dropMass) + " " + element.name,
                    dropped.transform
                );
            }
        }

        private static void InvokePlaySoundForSubstance(Element element, Vector3 position)
        {
            var method = typeof(WorldDamage).GetMethod("PlaySoundForSubstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                DebugConsole.LogWarning("[Multiplayer] Could not find PlaySoundForSubstance via reflection.");
                return;
            }

            var worldDamage = WorldDamage.Instance;

            if (worldDamage == null)
            {
                DebugConsole.LogWarning("[Multiplayer] WorldDamage.Instance is null.");
                return;
            }

            method.Invoke(worldDamage, new object[] { element, position });
        }

    }
}
