using System.Security.Principal;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using UnityEngine;

namespace ONI_MP.Patches.Navigation
{
    [HarmonyPatch(typeof(Navigator), nameof(Navigator.AdvancePath))]
    public static class NavigatorPatch
    {
        static bool Prefix(Navigator __instance)
        {
            if (!__instance.path.IsValid() || __instance.path.nodes == null || __instance.path.nodes.Count == 0)
                return true;

            // In Singleplayer
            if (!MultiplayerSession.InSession)
                return true;

            // Not networked. Allow
            if (!__instance.TryGetComponent<NetworkIdentity>(out var identity))
                return true;

            // Only the host can inform clients where to go
            if (MultiplayerSession.IsHost)
            {
                // We no longer need to sync the navigator
                //SendNavigationPacket(__instance, identity);
                //DebugNavigationPath(__instance);
                return true;
            }

            // Only move if they've been informed they can move
            /*if(__instance.GetCanAdvance())
            {
                return true;
            }*/

            return false;
        }

        private static void SendNavigationPacket(Navigator __instance, NetworkIdentity identity)
        {
            var packet = new NavigatorPathPacket
            {
                NetId = identity.NetId
            };

            foreach (var node in __instance.path.nodes)
            {
                packet.Steps.Add(new NavigatorPathPacket.PathStep
                {
                    Cell = node.cell,
                    NavType = node.navType,
                    TransitionId = node.transitionId
                });
            }

            PacketSender.SendToAll(packet);
        }

        private static void DebugNavigationPath(Navigator __instance)
        {
            string log = $"[Navigator AdvancePath] Sent path for {__instance.name} with {__instance.path.nodes.Count} steps.";
            for (int i = 0; i < __instance.path.nodes.Count; i++)
            {
                var node = __instance.path.nodes[i];
                Vector3 worldPos = Grid.CellToPosCBC(node.cell, Grid.SceneLayer.Move);
                log += $"\n  Step {i}: Cell={node.cell}, WorldPos={worldPos}, NavType={node.navType}, TransitionId={node.transitionId}";
            }

            DebugConsole.Log(log);
        }
    }

    [HarmonyPatch(typeof(Navigator), nameof(Navigator.GoTo), new[] {
    typeof(KMonoBehaviour), typeof(CellOffset[]), typeof(NavTactic)
})]
    public static class Navigator_GoTo_Target_Patch
    {
        static bool Prefix(Navigator __instance)
        {
            if (!MultiplayerSession.InSession)
            {
                return true; // Not in a multiplayer session, allow
            }

            if (__instance.TryGetComponent<NetworkIdentity>(out var netIdentity))
            {
                // Only allow host can initiate
                return MultiplayerSession.IsHost;
            }
            else
            {
                // A non networked object. Default to vanilla behavior
                return true;
            }
        }
    }
}
