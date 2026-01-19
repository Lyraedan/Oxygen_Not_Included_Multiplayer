using ONI_MP.Misc;
using ONI_MP.Networking.States;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class SteamNetworkingComponent : MonoBehaviour
	{
		public static UnityTaskScheduler scheduler = new UnityTaskScheduler();

		/*
		 * TODO:
		 * Update this class now that we can have different relay types. This is not steam specific anymore
		 * 
		 * **/

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

			if (NetworkConfig.relay.Equals(NetworkConfig.NetworkRelay.STEAM))
			{
				if (!SteamManager.Initialized)
					return;
			}

			if (!MultiplayerSession.InSession)
				return;

			if (MultiplayerSession.IsHost)
			{
				GameServer.Update();
			}
			else if (MultiplayerSession.IsClient && MultiplayerSession.HostUserID.IsValid())
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

			if (NetworkConfig.relay.Equals(NetworkConfig.NetworkRelay.STEAM))
			{
				SteamLobby.LeaveLobby();
			}
		}
	}
}
