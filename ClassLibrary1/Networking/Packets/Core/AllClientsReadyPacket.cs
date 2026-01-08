using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Core
{
	public class AllClientsReadyPacket : IPacket
	{

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
			DebugConsole.Log("[AllClientsReadyPacket] All players are ready! Closing overlay");
			ProcessAllReady();
		}

		public static void ProcessAllReady()
		{
			//CoroutineRunner.RunOne(CloseOverlayAfterDelay());
			MultiplayerOverlay.Show(STRINGS.UI.MP_OVERLAY.SYNC.FINALIZING_SYNC);
            MultiplayerOverlay.Close();
            SpeedControlScreen.Instance?.Unpause(false);
		}

		private static IEnumerator CloseOverlayAfterDelay()
		{
			MultiplayerOverlay.Show(STRINGS.UI.MP_OVERLAY.SYNC.FINALIZING_SYNC);
			yield return new WaitForSeconds(1f);
            MultiplayerOverlay.Close();
            SpeedControlScreen.Instance?.Unpause(false);
		}
	}
}
