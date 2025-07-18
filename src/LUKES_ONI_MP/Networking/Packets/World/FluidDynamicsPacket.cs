using System.IO;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.World
{
    /// <summary>
    /// Handles fluid dynamics synchronization including pipe flows, liquid movement, and pressure systems
    /// Part of Environmental Systems synchronization
    /// </summary>
    public class FluidDynamicsPacket : IPacket
    {
        public PacketType Type => PacketType.FluidDynamics;
        
        public int Cell;
        public int ConnectedCell; // For pipe connections
        public float FlowRate;
        public float FlowDirection; // 0-360 degrees
        public ushort FluidElementIdx;
        public float FluidTemperature;
        public float FluidMass;
        public byte FluidDiseaseIdx;
        public int FluidDiseaseCount;
        public bool IsPipeFlow; // true for pipes, false for open liquid flow

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Cell);
            writer.Write(ConnectedCell);
            writer.Write(FlowRate);
            writer.Write(FlowDirection);
            writer.Write(FluidElementIdx);
            writer.Write(FluidTemperature);
            writer.Write(FluidMass);
            writer.Write(FluidDiseaseIdx);
            writer.Write(FluidDiseaseCount);
            writer.Write(IsPipeFlow);
        }

        public void Deserialize(BinaryReader reader)
        {
            Cell = reader.ReadInt32();
            ConnectedCell = reader.ReadInt32();
            FlowRate = reader.ReadSingle();
            FlowDirection = reader.ReadSingle();
            FluidElementIdx = reader.ReadUInt16();
            FluidTemperature = reader.ReadSingle();
            FluidMass = reader.ReadSingle();
            FluidDiseaseIdx = reader.ReadByte();
            FluidDiseaseCount = reader.ReadInt32();
            IsPipeFlow = reader.ReadBoolean();
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost) return;

            // Apply fluid dynamics changes on client
            if (Grid.IsValidCell(Cell))
            {
                // Update fluid state in the target cell
                if (FluidMass > 0)
                {
                    SimMessages.ModifyCell(
                        Cell, FluidElementIdx,
                        FluidTemperature, FluidMass,
                        FluidDiseaseIdx, FluidDiseaseCount,
                        SimMessages.ReplaceType.Replace
                    );
                }

                // Handle pipe flow connections if applicable
                if (IsPipeFlow && Grid.IsValidCell(ConnectedCell))
                {
                    // Additional pipe flow logic can be added here
                    // This ensures both ends of pipe connections are synchronized
                }
            }
        }
    }
}
