using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Handshake;
using ONI_MP.Networking.Profiling;
using ONI_MP.Networking.States;
using Shared;
using Steamworks;
using System;
using System.Runtime.InteropServices;

namespace ONI_MP.Networking
{
	public static class GameServer
	{
		private static ServerState _state = ServerState.Stopped;
		public static ServerState State => _state;

		private static void SetState(ServerState newState)
		{
			if (_state != newState)
			{
				_state = newState;
				DebugConsole.Log($"[GameServer] State changed to: {_state}");
				Game.Instance?.Trigger(MP_HASHES.GameServer_OnStateChanged);
			}
		}

		public static void Start()
		{
			SetState(ServerState.Preparing);

            NetworkConfig.TransportServer.OnError = () => SetState(ServerState.Error);
            NetworkConfig.TransportServer.Prepare();

			SetState(ServerState.Starting);

			MultiplayerSession.IsHost = true;
			NetworkConfig.TransportServer.Start();

			DebugConsole.Log("[GameServer] Game Server started!");
			//MultiplayerSession.InSession = true;
			Game.Instance?.Trigger(MP_HASHES.OnConnected);
			Game.Instance?.Trigger(MP_HASHES.GameServer_OnServerStarted);
			//MultiplayerOverlay.Close();

			SetState(ServerState.Started);
		}

		public static void Shutdown()
		{
			SetState(ServerState.Stopped);

            NetworkConfig.TransportServer.CloseConnections();
			NetworkConfig.TransportServer.Stop();
            MultiplayerSession.IsHost = false;

            //MultiplayerSession.InSession = false;

            DebugConsole.Log("[GameServer] Shutdown complete.");
		}

		public static void Update()
		{
			switch (State)
			{
				case ServerState.Started:
					NetworkConfig.TransportServer.Update();
                    NetworkConfig.TransportServer.OnMessageRecieved();

                    // Check for lost chunks and retransmit specific missing chunks
                    SaveFileTransferManager.CheckForLostChunks();
					break;

				case ServerState.Preparing:
				case ServerState.Starting:
				case ServerState.Stopped:
				case ServerState.Error:
				default:
					// No server activity in these states.
					break;
			}
		}
	}
}
