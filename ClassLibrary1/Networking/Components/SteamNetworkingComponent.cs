using ONI_MP.Misc;
using ONI_MP.Networking.States;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class SteamNetworkingComponent : MonoBehaviour
	{
		public static UnityTaskScheduler scheduler = new UnityTaskScheduler();

		private void Start()
		{
			SteamNetworkingUtils.InitRelayNetworkAccess();
			GameClient.Init();

			// NOTE: Client reconnection after world load is now handled in 
			// GamePatch.OnSpawnPostfix which triggers AFTER the world is fully loaded.
			// This is safer than OnPostSceneLoaded which fires during scene unload.
		}

		private void Update()
		{
			scheduler.Tick();

			if (!SteamManager.Initialized)
				return;

			if (!MultiplayerSession.InSession)
				return;

			if (MultiplayerSession.IsHost)
			{
				GameServer.Update();
			}
			else if (MultiplayerSession.IsClient && MultiplayerSession.HostSteamID.IsValid())
			{
				GameClient.Poll();

				// Check for inactive transfers and request missing chunks
				ONI_MP.Misc.World.SaveChunkAssembler.CheckInactiveTransfers();
			}
		}

		private void OnApplicationQuit()
		{
			if (!MultiplayerSession.InSession)
				return;

			SteamLobby.LeaveLobby();
		}
	}
}
