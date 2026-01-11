using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using System.IO;

namespace ONI_MP.Networking.Packets.Core
{
	public class HardSyncPacket : IPacket
	{
		public void Serialize(BinaryWriter writer)
		{
			// No payload needed
		}

		public void Deserialize(BinaryReader reader)
		{
			// No payload needed
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
				return;

			// Hide all the player cursors on the client as they'll reappear as packets are recieved
			foreach (PlayerCursor cursor in MultiplayerSession.PlayerCursors.Values)
			{
				cursor.SetVisibility(false);
			}

			Sync();
			//PauseScreen.TriggerQuitGame();
		}

		public static void Sync()
		{
			GameClient.IsHardSyncInProgress = true;
			MultiplayerOverlay.Show(STRINGS.UI.MP_OVERLAY.SYNC.HARDSYNC_INPROGRESS);

			// This is incredibly stupid...
			GameClient.CacheCurrentServer();
			GameClient.Disconnect();

			PauseScreen.TriggerQuitGame(); // Force exit to frontend

			MultiplayerOverlay.Show(STRINGS.UI.MP_OVERLAY.SYNC.HARDSYNC_INPROGRESS);
			NetworkIdentityRegistry.Clear();
			GameClient.ReconnectFromCache();
		}
	}
}
