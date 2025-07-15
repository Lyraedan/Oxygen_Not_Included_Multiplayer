using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Cancel;
using Steamworks;
using System.Reflection;

namespace ONI_MP.Patches.ToolPatches.Cancel
{
    [HarmonyPatch(typeof(CancelTool), "OnDragTool")]
    [HarmonyPatch(new[] { typeof(int), typeof(int) })]
    public static class CancelToolPatch
    {
        public static void Postfix(int cell, int distFromOrigin)
        {
            if (!MultiplayerSession.InSession)
                return;

            var packet = new CancelPacket(cell, MultiplayerSession.LocalSteamID);

            if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(packet);
            }
            else
            {
                PacketSender.SendToHost(packet);
            }
        }
    }
}
