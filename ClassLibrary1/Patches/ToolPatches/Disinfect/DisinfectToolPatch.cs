using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Tools.Disinfect;
using ONI_MP.Profiling;

[HarmonyPatch(typeof(DisinfectTool), "OnDragTool")]
public class DisinfectToolPatch
{
    [HarmonyPrefix]
    public static void Prefix(int cell, int distFromOrigin)
    {
        Profiler.Active.Scope();

        if (!MultiplayerSession.InSession)
            return;

        if (DisinfectPacket.ProcessingIncoming)
            return;

        PacketSender.SendToAllOtherPeers(new DisinfectPacket { cell = cell, distFromOrigin = distFromOrigin });
    }
}