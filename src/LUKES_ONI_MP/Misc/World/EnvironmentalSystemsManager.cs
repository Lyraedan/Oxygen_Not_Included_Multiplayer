using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.World;
using UnityEngine;

namespace ONI_MP.Misc.World
{
    /// <summary>
    /// Manages environmental systems synchronization including gas flow, pressure, temperature, and fluid dynamics
    /// Coordinates real-time atmospheric changes across all multiplayer clients
    /// </summary>
    public static class EnvironmentalSystemsManager
    {
        private static readonly Queue<AtmosphericChangePacket> atmosphericQueue = new Queue<AtmosphericChangePacket>();
        private static readonly Queue<FluidDynamicsPacket> fluidQueue = new Queue<FluidDynamicsPacket>();
        private static float atmosphericUpdateTimer = 0f;
        private static float fluidUpdateTimer = 0f;
        private const float AtmosphericUpdateInterval = 2f; // Update atmospheric changes every 2 seconds
        private const float FluidUpdateInterval = 1f; // Update fluid dynamics every 1 second

        public static void QueueAtmosphericChange(int cell, float pressure, float density, ushort elementIdx, 
                                                 float temperature, byte diseaseIdx, int diseaseCount, bool isGas)
        {
            if (!MultiplayerSession.IsHost) return;

            lock (atmosphericQueue)
            {
                atmosphericQueue.Enqueue(new AtmosphericChangePacket
                {
                    Cell = cell,
                    Pressure = pressure,
                    Density = density,
                    ElementIdx = elementIdx,
                    Temperature = temperature,
                    DiseaseIdx = diseaseIdx,
                    DiseaseCount = diseaseCount,
                    IsGas = isGas
                });
            }
        }

        public static void QueueFluidDynamics(int cell, int connectedCell, float flowRate, float flowDirection,
                                            ushort fluidElementIdx, float fluidTemperature, float fluidMass,
                                            byte fluidDiseaseIdx, int fluidDiseaseCount, bool isPipeFlow)
        {
            if (!MultiplayerSession.IsHost) return;

            lock (fluidQueue)
            {
                fluidQueue.Enqueue(new FluidDynamicsPacket
                {
                    Cell = cell,
                    ConnectedCell = connectedCell,
                    FlowRate = flowRate,
                    FlowDirection = flowDirection,
                    FluidElementIdx = fluidElementIdx,
                    FluidTemperature = fluidTemperature,
                    FluidMass = fluidMass,
                    FluidDiseaseIdx = fluidDiseaseIdx,
                    FluidDiseaseCount = fluidDiseaseCount,
                    IsPipeFlow = isPipeFlow
                });
            }
        }

        public static void Update()
        {
            if (!MultiplayerSession.IsHost) return;

            // Update atmospheric changes
            atmosphericUpdateTimer += Time.unscaledDeltaTime;
            if (atmosphericUpdateTimer >= AtmosphericUpdateInterval)
            {
                FlushAtmosphericChanges();
                atmosphericUpdateTimer = 0f;
            }

            // Update fluid dynamics
            fluidUpdateTimer += Time.unscaledDeltaTime;
            if (fluidUpdateTimer >= FluidUpdateInterval)
            {
                FlushFluidDynamics();
                fluidUpdateTimer = 0f;
            }
        }

        private static void FlushAtmosphericChanges()
        {
            lock (atmosphericQueue)
            {
                if (atmosphericQueue.Count == 0) return;

                int count = 0;
                while (atmosphericQueue.Count > 0 && count < 50) // Limit to 50 atmospheric changes per update
                {
                    var packet = atmosphericQueue.Dequeue();
                    PacketSender.SendToAll(packet);
                    count++;
                }

                if (count > 0)
                {
                    DebugConsole.Log($"[Environmental Systems] Synchronized {count} atmospheric changes (gas pressure, composition)");
                }
            }
        }

        private static void FlushFluidDynamics()
        {
            lock (fluidQueue)
            {
                if (fluidQueue.Count == 0) return;

                int count = 0;
                while (fluidQueue.Count > 0 && count < 30) // Limit to 30 fluid dynamics updates per update
                {
                    var packet = fluidQueue.Dequeue();
                    PacketSender.SendToAll(packet);
                    count++;
                }

                if (count > 0)
                {
                    DebugConsole.Log($"[Environmental Systems] Synchronized {count} fluid dynamics (pipes, liquid flow)");
                }
            }
        }

        /// <summary>
        /// Analyzes a cell to determine if it contains gas elements that need atmospheric synchronization
        /// </summary>
        public static bool IsGasCell(int cell)
        {
            if (!Grid.IsValidCell(cell)) return false;
            
            var element = Grid.Element[cell];
            return element.IsGas;
        }

        /// <summary>
        /// Analyzes a cell to determine if it contains liquid elements that need fluid dynamics synchronization
        /// </summary>
        public static bool IsLiquidCell(int cell)
        {
            if (!Grid.IsValidCell(cell)) return false;
            
            var element = Grid.Element[cell];
            return element.IsLiquid;
        }

        /// <summary>
        /// Gets the current atmospheric pressure at a specific cell
        /// </summary>
        public static float GetPressure(int cell)
        {
            if (!Grid.IsValidCell(cell)) return 0f;
            
            return Grid.Mass[cell]; // In ONI, mass represents pressure for gases
        }

        /// <summary>
        /// Gets the current temperature at a specific cell
        /// </summary>
        public static float GetTemperature(int cell)
        {
            if (!Grid.IsValidCell(cell)) return 0f;
            
            return Grid.Temperature[cell];
        }
    }
}
