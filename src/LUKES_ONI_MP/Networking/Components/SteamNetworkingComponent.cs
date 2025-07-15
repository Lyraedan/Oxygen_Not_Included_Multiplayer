using UnityEngine;
using ONI_MP.DebugTools;
using Steamworks;
using ONI_MP.Misc;
using ONI_MP.Networking.States;
using ONI_MP.Menus;

namespace ONI_MP.Networking.Components
{
    public class SteamNetworkingComponent : MonoBehaviour
    {
        public static UnityTaskScheduler scheduler = new UnityTaskScheduler();

        private void Start()
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            GameClient.Init();

            MultiplayerMod.OnPostSceneLoaded += () =>
            {
                if (GameClient.State.Equals(ClientState.LoadingWorld))
                {
                    GameClient.ReconnectFromCache();
                    MultiplayerOverlay.Close();
                }
            };
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
