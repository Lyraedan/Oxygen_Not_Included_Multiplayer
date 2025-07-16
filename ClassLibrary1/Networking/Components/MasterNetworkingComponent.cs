using UnityEngine;
using ONI_MP.DebugTools;
using Steamworks;
using ONI_MP.Misc;
using ONI_MP.Networking.States;
using ONI_MP.Menus;
using ONI_MP.Networking.Relay.Platforms.Steam;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Components
{
    public class MasterNetworkingComponent : MonoBehaviour
    {
        public static UnityTaskScheduler scheduler = new UnityTaskScheduler();

        private void Start()
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            PacketSender.Platform.GameClient.Init();

            MultiplayerMod.OnPostSceneLoaded += () =>
            {
                if (PacketSender.Platform.GameClient.State.Equals(ClientState.LoadingWorld))
                {
                    PacketSender.Platform.GameClient.ReconnectFromCache();
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
                PacketSender.Platform.GameServer.Update();
            }
            else if (MultiplayerSession.IsClient && !string.IsNullOrEmpty(MultiplayerSession.HostId))
            {
                PacketSender.Platform.GameClient.Poll();
            }
        }

        private void OnApplicationQuit()
        {
            if (!MultiplayerSession.InSession)
                return;

            PacketSender.Platform.Lobby.LeaveLobby();
        }
    }
}
