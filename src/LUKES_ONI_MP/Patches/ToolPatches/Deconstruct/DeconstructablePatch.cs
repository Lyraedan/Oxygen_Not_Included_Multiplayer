using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Deconstruct;

namespace ONI_MP.Patches.ToolPatches.Deconstruct
{
    [HarmonyPatch(typeof(Deconstructable), "OnCompleteWork")]
    public static class DeconstructablePatch
    {
        public static void Postfix(Deconstructable __instance)
        {
            if (!MultiplayerSession.IsHost || !MultiplayerSession.InSession)
                return;

            int cell = Grid.PosToCell(__instance.transform.position);
            var packet = new DeconstructCompletePacket { Cell = cell };
            PacketSender.SendToAllClients(packet);

            DebugConsole.Log($"[DeconstructComplete] Host sent DeconstructCompletePacket for cell {cell}");
        }
    }
}
