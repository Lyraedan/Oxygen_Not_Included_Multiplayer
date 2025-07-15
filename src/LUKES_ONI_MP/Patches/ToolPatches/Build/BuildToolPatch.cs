using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Build;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Patches.ToolPatches.Build
{
    [HarmonyPatch(typeof(BuildTool), "TryBuild")]
    public static class BuildToolPatch
    {
        static void Prefix(BuildTool __instance, int cell)
        {
            var def = AccessTools.Field(typeof(BuildTool), "def").GetValue(__instance) as BuildingDef;
            if (def != null)
            {
                DebugConsole.Log($"[BuildTool] Attempting to build: {def.PrefabID} at cell {cell}");
            }
        }

        static void Postfix(BuildTool __instance, int cell)
        {
            if (!MultiplayerSession.InSession || __instance == null)
                return;

            var def = AccessTools.Field(typeof(BuildTool), "def").GetValue(__instance) as BuildingDef;
            var selectedElements = AccessTools.Field(typeof(BuildTool), "selectedElements")
                                              .GetValue(__instance) as IList<Tag>;
            var orientation = __instance.GetBuildingOrientation;

            if (def == null || selectedElements == null)
                return;

            // Log result
            GameObject obj = Grid.Objects[cell, (int)def.ObjectLayer];
            if (obj != null)
            {
                DebugConsole.Log($"[BuildTool] Successfully placed {def.PrefabID} at cell {cell}");
            }
            else
            {
                DebugConsole.Log($"[BuildTool] Failed to place {def.PrefabID} at cell {cell}");
                return;
            }

            // Create and send packet
            var packet = new BuildPacket(
                def.PrefabID,
                cell,
                orientation,
                selectedElements,
                MultiplayerSession.LocalSteamID
            );

            if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(packet);
                DebugConsole.Log($"[Build] Host sent BuildPacket to all clients for {def.PrefabID} at {cell}");
            }
            else
            {
                PacketSender.SendToHost(packet);
                DebugConsole.Log($"[Build] Client sent BuildPacket to host for {def.PrefabID} at {cell}");
            }
        }
    }
}
