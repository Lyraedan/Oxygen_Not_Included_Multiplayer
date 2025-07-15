using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Wire;
using Steamworks;
using System.Collections;
using System.Reflection;

namespace ONI_MP.Patches.ToolPatches.Wire
{
    [HarmonyPatch(typeof(WireBuildTool), "ApplyPathToConduitSystem")]
    public static class WireBuildToolPatch
    {
        public static void Prefix(WireBuildTool __instance)
        {
            if (!MultiplayerSession.InSession)
                return;

            var pathField = typeof(BaseUtilityBuildTool)
                .GetField("path", BindingFlags.Instance | BindingFlags.NonPublic);

            if (pathField == null)
            {
                DebugConsole.LogError("[WirePatch] Failed to reflect 'path' from BaseUtilityBuildTool");
                return;
            }

            var path = (IList)pathField.GetValue(__instance);
            if (path == null || path.Count < 2)
                return;

            var packet = new WireBuildPacket();
            packet.SenderId = MultiplayerSession.LocalSteamID;

            foreach (var node in path)
            {
                var nodeType = node.GetType();
                int cell = (int)nodeType.GetField("cell").GetValue(node);
                bool valid = (bool)nodeType.GetField("valid").GetValue(node);

                packet.Path.Add(new WireBuildPacket.Node
                {
                    Cell = cell,
                    Valid = valid
                });
            }

            if (MultiplayerSession.IsHost)
                PacketSender.SendToAllClients(packet);
            else
                PacketSender.SendToHost(packet);
        }
    }
}
