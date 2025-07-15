using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Prioritize;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch(typeof(PrioritizeTool), "TryPrioritizeGameObject")]
public static class PrioritizeToolPatch
{
    public static void Postfix(GameObject target, PrioritySetting priority, bool __result)
    {
        if (!MultiplayerSession.InSession || !__result)
            return;

        // Find the grid cell of the object
        int cell = Grid.PosToCell(target);
        if (!Grid.IsValidCell(cell)) return;

        var packet = new PrioritizePacket
        {
            TargetCells = new List<int> { cell },
            Priority = priority,
            SenderId = MultiplayerSession.LocalSteamID
        };

        if (MultiplayerSession.IsHost)
        {
            PacketSender.SendToAllClients(packet);
            DebugConsole.Log($"[Host] Rebroadcasted priority for cell {cell}");
        }
        else
        {
            PacketSender.SendToHost(packet);
            DebugConsole.Log($"[Client] Sent PrioritizePacket for cell {cell} to host");
        }
    }
}
