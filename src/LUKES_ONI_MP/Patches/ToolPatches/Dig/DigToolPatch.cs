using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Dig;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Patches.ToolPatches.Dig
{
    [HarmonyPatch(typeof(DigTool), nameof(DigTool.PlaceDig))]
    public static class DigTool_PlaceDig_Patch
    {
        public static void Postfix(int cell, int animationDelay, GameObject __result)
        {

            if (!MultiplayerSession.InSession)
            {
                DebugConsole.LogWarning("[PlaceDig Patch] Skipped: MultiplayerSession.InSession is false");
                return;
            }

            if (__result == null)
            {
                DebugConsole.LogWarning($"[PlaceDig Patch] Skipped: __result is null for cell {cell}");
                return;
            }

            //if (Diggable.GetDiggable(cell) != null)
            //{
            //    DebugConsole.LogWarning($"[PlaceDig Patch] Skipped: Cell {cell} is already diggable");
            //    return;
            //}

            var packet = new DiggablePacket()
            {
                Cell = cell,
                SenderId = MultiplayerSession.LocalSteamID
            };

            if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(packet);
                DebugConsole.Log($"[Chores/Dig] Host sent DiggablePacket to all for cell {cell}");
            }
            else
            {
                PacketSender.SendToHost(packet);
                DebugConsole.Log($"[Chores/Dig] Client sent DiggablePacket to host for cell {cell}");
            }
        }
    }
}
