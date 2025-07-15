using HarmonyLib;
using UnityEngine;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.DebugTools;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(SpeedControlScreen))]
    public static class SpeedControlScreen_SendSpeedPacketPatch
    {
        [HarmonyPatch("SetSpeed")]
        [HarmonyPostfix]
        public static void SetSpeed_Postfix(int Speed)
        {
            /*
            var packet = new SpeedChangePacket((SpeedChangePacket.SpeedState)Speed);

            if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(packet);
            } else
            {
                PacketSender.SendToHost(packet);
            }
            DebugConsole.Log($"[SpeedControl] Sent SpeedChangePacket: {packet.Speed}");*/
        }

        [HarmonyPatch("TogglePause")]
        [HarmonyPostfix]
        public static void TogglePause_Postfix(SpeedControlScreen __instance)
        {
            /*
            var speedState = __instance.IsPaused
                ? SpeedChangePacket.SpeedState.Paused
                : (SpeedChangePacket.SpeedState)__instance.GetSpeed();

            var packet = new SpeedChangePacket(speedState);
            if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(packet);
            }
            else
            {
                PacketSender.SendToHost(packet);
            }
            DebugConsole.Log($"[SpeedControl] Sent SpeedChangePacket (pause toggle): {packet.Speed}");*/
        }
    }
}
