using UnityEngine;
using ONI_MP.DebugTools;
using Steamworks;
using ONI_MP.Misc;
using ONI_MP.Networking.States;
using ONI_MP.Menus;
using ONI_MP.Networking.Relay.Platforms.Steam;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay.Platforms.EOS;

namespace ONI_MP.Networking.Components
{
    public class MasterNetworkingComponent : MonoBehaviour
    {
        public static UnityTaskScheduler scheduler = new UnityTaskScheduler();

        int platform = 0; // Steam

        private void Start()
        {
            
        }

        public void Init()
        {
            int platform = Configuration.GetClientProperty<int>("Platform");
            if (platform == 0)
            {
                // Use Steam
                SteamNetworkingUtils.InitRelayNetworkAccess();
            }
            this.platform = platform;

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

            if (platform == 1) // EOS
            {
                TickEOS();
            }

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

        public static void TickEOS()
        {
            EOSManager.Instance?.GetPlatformInterface()?.Tick();
        }

        private void OnApplicationQuit()
        {
            if (!MultiplayerSession.InSession)
                return;

            PacketSender.Platform.Lobby.LeaveLobby();
        }
    }
}
