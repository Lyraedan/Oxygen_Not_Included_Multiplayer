using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Clear;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;
namespace ONI_MP.Patches.ToolPatches.Clear
{
    [HarmonyPatch(typeof(ClearTool), "OnDragTool")]
    public static class ClearTool_OnDragTool_Patch
    {
        public static void Prefix(int cell)
        {
            // Only send packet if we're a client
            if (!MultiplayerSession.InSession)
                return;

            if (!Grid.IsValidCell(cell))
                return;

            // Check for clearable networked objects
            List<int> cellsToClear = new List<int>();
            GameObject gameObject = Grid.Objects[cell, 3];
            if (gameObject == null)
                return;

            bool foundValidTarget = false;

            ObjectLayerListItem item = gameObject.GetComponent<Pickupable>()?.objectLayerListItem;
            while (item != null)
            {
                GameObject target = item.gameObject;
                item = item.nextItem;

                if (target == null) continue;
                if (target.GetComponent<MinionIdentity>() != null) 
                    continue;
                if (!target.TryGetComponent<Clearable>(out var clearable) || !clearable.isClearable)
                    continue;
                if (!target.TryGetComponent<NetworkIdentity>(out _))
                    continue;

                foundValidTarget = true;
                break;
            }

            if (foundValidTarget)
            {
                var packet = new ClearPacket
                {
                    SenderId = MultiplayerSession.LocalSteamID,
                    TargetCells = new List<int> { cell },
                    ActionType = ClearActionType.Sweep
                };

                if(MultiplayerSession.IsHost)
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

}
