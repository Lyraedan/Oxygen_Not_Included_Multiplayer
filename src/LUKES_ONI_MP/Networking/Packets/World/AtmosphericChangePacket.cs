using System.IO;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.World
{
    /// <summary>
    /// Handles real-time atmospheric changes including gas composition, pressure, and air quality
    /// Part of Environmental Systems synchronization
    /// </summary>
    public class AtmosphericChangePacket : IPacket
    {
        public PacketType Type => PacketType.AtmosphericChange;
        
        public int Cell;
        public float Pressure;
        public float Density;
        public ushort ElementIdx;
        public float Temperature;
        public byte DiseaseIdx;
        public int DiseaseCount;
        public bool IsGas;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Cell);
            writer.Write(Pressure);
            writer.Write(Density);
            writer.Write(ElementIdx);
            writer.Write(Temperature);
            writer.Write(DiseaseIdx);
            writer.Write(DiseaseCount);
            writer.Write(IsGas);
        }

        public void Deserialize(BinaryReader reader)
        {
            Cell = reader.ReadInt32();
            Pressure = reader.ReadSingle();
            Density = reader.ReadSingle();
            ElementIdx = reader.ReadUInt16();
            Temperature = reader.ReadSingle();
            DiseaseIdx = reader.ReadByte();
            DiseaseCount = reader.ReadInt32();
            IsGas = reader.ReadBoolean();
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost) return;

            // Apply atmospheric changes on client
            if (Grid.IsValidCell(Cell))
            {
                // For gas elements, ensure proper atmospheric simulation
                if (IsGas)
                {
                    // Update gas pressure and composition
                    SimMessages.ModifyCell(
                        Cell, ElementIdx,
                        Temperature, Density,
                        DiseaseIdx, DiseaseCount,
                        SimMessages.ReplaceType.Replace
                    );
                }
            }
        }
    }
}
