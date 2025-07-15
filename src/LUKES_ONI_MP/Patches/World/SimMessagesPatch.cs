using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;

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

            /* Disable for now whilst I focus on getting saves syncing
            WorldUpdateBatcher.Queue(new WorldUpdatePacket.CellUpdate
            {
                Cell = gameCell,
                ElementIdx = elementIdx,
                Temperature = temperature,
                Mass = mass,
                DiseaseIdx = disease_idx,
                DiseaseCount = disease_count
            });*/
        }
    }
}
