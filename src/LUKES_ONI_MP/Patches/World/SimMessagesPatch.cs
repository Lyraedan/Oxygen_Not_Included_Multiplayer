using HarmonyLib;
using ONI_MP.Misc.World;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch(typeof(SimMessages), nameof(SimMessages.ModifyCell))]
    public static class SimMessagesPatch
    {
        [HarmonyPrefix]
        public static void Prefix(
            int gameCell,
            ushort elementIdx,
            float temperature,
            float mass,
            byte disease_idx,
            int disease_count,
            SimMessages.ReplaceType replace_type,
            bool do_vertical_solid_displacement,
            int callbackIdx
        )
        {
            if (!MultiplayerSession.IsHost || !Grid.IsValidCell(gameCell)) return;

            // Environmental Systems: Real-time synchronization of gas flow, pressure, temperature, and fluid dynamics
            WorldUpdateBatcher.Queue(new WorldUpdatePacket.CellUpdate
            {
                Cell = gameCell,
                ElementIdx = elementIdx,
                Temperature = temperature,
                Mass = mass,
                DiseaseIdx = disease_idx,
                DiseaseCount = disease_count
            });

            // Enhanced Environmental Systems: Specific atmospheric and fluid dynamics tracking
            var element = ElementLoader.elements[elementIdx];
            
            if (element.IsGas)
            {
                // Queue atmospheric changes for gas elements (oxygen, CO2, hydrogen, etc.)
                EnvironmentalSystemsManager.QueueAtmosphericChange(
                    gameCell, mass, mass, elementIdx, temperature, disease_idx, disease_count, true);
            }
            else if (element.IsLiquid)
            {
                // Queue fluid dynamics for liquid elements (water, polluted water, etc.)
                EnvironmentalSystemsManager.QueueFluidDynamics(
                    gameCell, -1, mass, 0f, elementIdx, temperature, mass, disease_idx, disease_count, false);
            }
        }
    }
}
