using System.Collections;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Core
{
    public class AllClientsReadyPacket : IPacket
    {
        public PacketType Type => PacketType.AllClientsReady;

        public void Serialize(BinaryWriter writer)
        {
            // No payload needed for now
        }

        public void Deserialize(BinaryReader reader)
        {
            // No payload to read
        }

        public void OnDispatched()
        {
            DebugConsole.Log("[AllClientsReadyPacket] All players are ready! Closing overlay in 1 second...");
            ProcessAllReady();
        }

        public static void ProcessAllReady()
        {
            //CoroutineRunner.RunOne(CloseOverlayAfterDelay());
            MultiplayerOverlay.Show("All players are ready!\nPlease wait...");
            SpeedControlScreen.Instance?.Unpause(false);
            MultiplayerOverlay.Close();
        }

        private static IEnumerator CloseOverlayAfterDelay()
        {
            MultiplayerOverlay.Show("All players are ready!\nPlease wait...");
            yield return new WaitForSeconds(1f);
            SpeedControlScreen.Instance?.Unpause(false);
            MultiplayerOverlay.Close();
        }
    }
}
