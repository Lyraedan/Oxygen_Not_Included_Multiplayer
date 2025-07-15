using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Tools.Move;

namespace ONI_MP.Patches.ToolPatches.Move
{
    [HarmonyPatch(typeof(MoveToLocationTool), "SetMoveToLocation")]
    public static class MoveToLocationToolPatch
    {
        public static bool Prefix(MoveToLocationTool __instance, int target_cell)
        {
            if (!MultiplayerSession.InSession || MultiplayerSession.IsHost)
                return true;

            var nav = AccessTools.Field(typeof(MoveToLocationTool), "targetNavigator").GetValue(__instance) as Navigator;
            var movable = AccessTools.Field(typeof(MoveToLocationTool), "targetMovable").GetValue(__instance) as Movable;
            var go = nav?.gameObject ?? movable?.gameObject;

            if (go == null || !go.TryGetComponent<NetworkIdentity>(out var identity))
            {
                DebugConsole.LogWarning("[Client] Cannot send move request: no NetworkIdentity.");
                return false;
            }

            // Send move request to host
            var packet = new MoveToLocationPacket
            {
                Cell = target_cell,
                TargetNetId = identity.NetId,
                SenderId = MultiplayerSession.LocalSteamID
            };

            PacketSender.SendToHost(packet);
            DebugConsole.Log($"[Client] Sent MoveToLocationPacket to host for NetId {identity.NetId} to move to {target_cell}");

            return false;
        }
    }
}
