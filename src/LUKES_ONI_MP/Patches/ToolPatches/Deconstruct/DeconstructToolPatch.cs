using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Deconstruct;
using Steamworks;

namespace ONI_MP.Patches.ToolPatches.Deconstruct
{
    [HarmonyPatch(typeof(DeconstructTool), "OnDragTool")]
    [HarmonyPatch(new[] { typeof(int), typeof(int) })]
    public static class DeconstructToolPatch
    {
        public static void Postfix(int cell, int distFromOrigin)
        {
            if (!MultiplayerSession.InSession)
                return;

            var packet = new DeconstructPacket(cell, MultiplayerSession.LocalSteamID);

            if (MultiplayerSession.IsHost)
                PacketSender.SendToAllClients(packet);
            else
                PacketSender.SendToHost(packet);
        }
    }
}
