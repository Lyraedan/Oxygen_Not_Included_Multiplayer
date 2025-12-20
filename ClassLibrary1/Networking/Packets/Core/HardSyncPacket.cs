using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.Menus;
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
			MultiplayerOverlay.Show("Hard sync in progress!");

			// This is incredibly stupid...
			GameClient.CacheCurrentServer();
			GameClient.Disconnect();

			PauseScreen.TriggerQuitGame(); // Force exit to frontend

			MultiplayerOverlay.Show("Hard sync in process!");
			NetworkIdentityRegistry.Clear();
			GameClient.ReconnectFromCache();
		}
	}
}
