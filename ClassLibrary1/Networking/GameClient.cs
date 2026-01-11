using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Handshake;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.States;
using ONI_MP.Patches.ToolPatches;
using Shared;
using Shared.Helpers;
using Steamworks;
using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ONI_MP.Networking
{
	public static class GameClient
	{
        private static Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;
		public static HSteamNetConnection? Connection { get; private set; }

		private static ClientState _state = ClientState.Disconnected;
		public static ClientState State => _state;

		private static bool _pollingPaused = false;

		private static CachedConnectionInfo? _cachedConnectionInfo = null;

		public static bool IsHardSyncInProgress = false;
		private static bool _modVerificationSent = false;

		private static SteamNetConnectionRealTimeStatus_t? connectionHealth = null;

		private struct CachedConnectionInfo
		{
			public CSteamID HostSteamID;

			public CachedConnectionInfo(CSteamID id)
			{
				HostSteamID = id;
			}
		}

		/// <summary>
		/// Returns true if we have cached connection info from a previous session
		/// (used to determine if we need to reconnect after world load)
		/// </summary>
		public static bool HasCachedConnection()
		{
			return _cachedConnectionInfo.HasValue;
		}

		/// <summary>
		/// Clears the cached connection info after successful reconnection or on error
		/// </summary>
		public static void ClearCachedConnection()
		{
			_cachedConnectionInfo = null;
		}

		public static void SetState(ClientState newState)
		{
			if (_state != newState)
			{
				_state = newState;
				DebugConsole.Log($"[GameClient] State changed to: {_state}");					
			}
		}

		public static void Init()
		{
			if (_connectionStatusChangedCallback == null)
			{
				_connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
				DebugConsole.Log("[GameClient] Registered connection status callback.");
			}
		}

		public static void ConnectToHost(CSteamID hostSteamId, bool showLoadingScreen = true)
		{
			// Reset mod verification for new connection attempts
			_modVerificationSent = false;

			if (showLoadingScreen)
			{
				MultiplayerOverlay.Show(string.Format(STRINGS.UI.MP_OVERLAY.CLIENT.CONNECTING_TO_HOST, SteamFriends.GetFriendPersonaName(hostSteamId)));
			}

			DebugConsole.Log($"[GameClient] Attempting ConnectP2P to host {hostSteamId}...");
			SetState(ClientState.Connecting);

			var identity = new SteamNetworkingIdentity();
			identity.SetSteamID64(hostSteamId.m_SteamID);

			Connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, null);
			DebugConsole.Log($"[GameClient] ConnectP2P returned handle: {Connection.Value.m_HSteamNetConnection}");
		}

		public static void Disconnect()
		{
			if (Connection.HasValue)
			{
				DebugConsole.Log("[GameClient] Disconnecting from host...");

				bool result = SteamNetworkingSockets.CloseConnection(
						Connection.Value,
						0,
						"Client disconnecting",
						false
				);

				DebugConsole.Log($"[GameClient] CloseConnection result: {result}");
				Connection = null;
				SetState(ClientState.Disconnected);
				MultiplayerSession.InSession = false;
				//SaveHelper.CaptureWorldSnapshot();
			}
			else
			{
				DebugConsole.LogWarning("[GameClient] Disconnect called, but no connection exists.");
			}
		}

		public static void ReconnectToSession()
		{
			if (Connection.HasValue || State == ClientState.Connected || State == ClientState.Connecting)
			{
				DebugConsole.Log("[GameClient] Reconnecting: First disconnecting existing connection.");
				Disconnect();
				System.Threading.Thread.Sleep(100);
			}

			if (MultiplayerSession.HostSteamID != CSteamID.Nil)
			{
				DebugConsole.Log("[GameClient] Attempting to reconnect to host...");
				ConnectToHost(MultiplayerSession.HostSteamID);
			}
			else
			{
				DebugConsole.LogWarning("[GameClient] Cannot reconnect: HostSteamID is not set.");
			}
		}

		public static void Poll()
		{
			if (_pollingPaused)
				return;

			SteamNetworkingSockets.RunCallbacks();
			EvaluateConnectionHealth();

			switch (State)
			{
				case ClientState.Connected:
				case ClientState.InGame:
					if (Connection.HasValue)
						ProcessIncomingMessages(Connection.Value);
					else
						DebugConsole.LogWarning($"[GameClient] Poll() - Connection is null! State: {State}");
					break;
				case ClientState.Connecting:
				case ClientState.Disconnected:
				case ClientState.Error:
				default:
					break;
			}
		}

		private static void ProcessIncomingMessages(HSteamNetConnection conn)
		{
			int maxMessagesPerConnectionPoll = Configuration.GetClientProperty<int>("MaxMessagesPerPoll");
			IntPtr[] messages = new IntPtr[maxMessagesPerConnectionPoll];
			int msgCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, messages, maxMessagesPerConnectionPoll);

			//if (msgCount > 0)
			//{
			//	DebugConsole.Log($"[GameClient] ProcessIncomingMessages() - Received {msgCount} messages");
			//}

			for (int i = 0; i < msgCount; i++)
			{
				var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(messages[i]);
				byte[] data = new byte[msg.m_cbSize];
				Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);

				try
				{
					//DebugConsole.Log($"[GameClient] Processing packet {i+1}/{msgCount}, size: {msg.m_cbSize} bytes, readyToProcess: {PacketHandler.readyToProcess}");
					PacketHandler.HandleIncoming(data);
				}
				catch (Exception ex)
				{
					DebugConsole.LogWarning($"[GameClient] Failed to handle incoming packet: {ex}"); // Prevent crashes from packet handling
				}

				SteamNetworkingMessage_t.Release(messages[i]);
			}
		}

		private static void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
		{
			var state = data.m_info.m_eState;
			var remote = data.m_info.m_identityRemote.GetSteamID();

			DebugConsole.Log($"[GameClient] Connection status changed: {state} (remote={remote})");

			if (Connection.HasValue && data.m_hConn.m_HSteamNetConnection != Connection.Value.m_HSteamNetConnection)
				return;

			switch (state)
			{
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
					OnConnected();
					break;
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
					OnDisconnected("Closed by peer or problem detected locally", remote, state);
					break;
				default:
					break;
			}
		}

		public static void OnHostResponseReceived(GameStateRequestPacket packet)
		{
			DebugConsole.Log("Gamestate packet received");
			MP_Timer.Instance.Abort();
			if (!SaveHelper.SavegameDlcListValid(packet.ActiveDlcIds, out var errorMsg))
			{
				DebugConsole.Log("invalid dlc config detected");
				SaveHelper.ShowMessageAndReturnToMainMenu(errorMsg);
				return;
			}

			if (!SaveHelper.SteamModListSynced(packet.ActiveModIds, out var notEnabled, out var notDisabled, out var missingMods))
			{
				string text = STRINGS.UI.MP_OVERLAY.SYNC.MODSYNC.TEXT + "\n\n";
				if (notEnabled.Any())
					text += string.Format(STRINGS.UI.MP_OVERLAY.SYNC.MODSYNC.TOENABLE, notEnabled.Count) +"\n";
				if (notDisabled.Any())
					text += string.Format(STRINGS.UI.MP_OVERLAY.SYNC.MODSYNC.TODISABLE, notDisabled.Count) + "\n";
				if (missingMods.Any())
					text += string.Format(STRINGS.UI.MP_OVERLAY.SYNC.MODSYNC.MISSING, missingMods.Count) + "\n";


				DialogUtil.CreateConfirmDialogFrontend(STRINGS.UI.MP_OVERLAY.SYNC.MODSYNC.TITLE, text,
	   STRINGS.UI.MP_OVERLAY.SYNC.MODSYNC.CONFIRM_SYNC,
				() => { SaveHelper.SyncModsAndRestart(notEnabled, notDisabled, missingMods); },
				STRINGS.UI.MP_OVERLAY.SYNC.MODSYNC.CANCEL,
				BackToMainMenu,
				STRINGS.UI.MP_OVERLAY.SYNC.MODSYNC.DENY_SYNC,
				ContinueConnectionFlow);
				DebugConsole.Log("mods not synced!");
				return;
			}

			ContinueConnectionFlow();
		}
		static void BackToMainMenu()
		{
			MultiplayerOverlay.Close();
			NetworkIdentityRegistry.Clear();
			SteamLobby.LeaveLobby();
			App.LoadScene("frontend");
		}

		private static void OnConnected()
		{
			//MultiplayerOverlay.Close();

			// We've reconnected in game
			MultiplayerSession.InSession = true;
            Game.Instance?.Trigger(MP_HASHES.OnConnected);
            SetState(ClientState.Connected);

            var hostId = MultiplayerSession.HostSteamID;
			if (!MultiplayerSession.ConnectedPlayers.ContainsKey(hostId))
			{
				var hostPlayer = new MultiplayerPlayer(hostId);
				MultiplayerSession.ConnectedPlayers[hostId] = hostPlayer;
			}

			// Store the connection handle for host
			MultiplayerSession.ConnectedPlayers[hostId].Connection = Connection;

			DebugConsole.Log("[GameClient] Connection to host established!");

            // Skip mod verification if we are the host
            if (MultiplayerSession.IsHost)
			{
				return;
			}

			PacketHandler.readyToProcess = true;

			if (Utils.IsInGame())
			{
				ContinueConnectionFlow();
			}
			else
			{
				PacketSender.SendToHost(new GameStateRequestPacket(MultiplayerSession.LocalSteamID));
				MP_Timer.Instance.StartDelayedAction(10, () => CoroutineRunner.RunOne(ShowMessageAndReturnToTitle()));
			}
        }

        private static void ContinueConnectionFlow()
		{
			// CRITICAL: Only execute on client, never on server
			if (MultiplayerSession.IsHost)
			{
				DebugConsole.Log("[GameClient] ContinueConnectionFlow called on host - ignoring");
				return;
			}

			DebugConsole.Log($"[GameClient] ContinueConnectionFlow - IsInMenu: {Utils.IsInMenu()}, IsInGame: {Utils.IsInGame()}, HardSyncInProgress: {IsHardSyncInProgress}");

			ReadyManager.SendReadyStatusPacket(ClientReadyState.Unready);

			if (Utils.IsInMenu())
			{
				DebugConsole.Log("[GameClient] Client is in menu - requesting save file or sending ready status");

				// CRITICAL: Enable packet processing BEFORE requesting save file
				// Otherwise, host packets will be discarded!
				PacketHandler.readyToProcess = true;
				DebugConsole.Log("[GameClient] PacketHandler.readyToProcess = true (menu)");

				// Show overlay with localized message
				MultiplayerOverlay.Show(string.Format(STRINGS.UI.MP_OVERLAY.CLIENT.WAITING_FOR_PLAYER, SteamFriends.GetFriendPersonaName(MultiplayerSession.HostSteamID)));
				if (!IsHardSyncInProgress)
				{
					DebugConsole.Log("[GameClient] Requesting save file from host");
					var packet = new SaveFileRequestPacket
					{
						Requester = MultiplayerSession.LocalSteamID
					};
					PacketSender.SendToHost(packet);
				}
				else
				{
					DebugConsole.Log("[GameClient] Hard sync in progress, sending ready status");
					// Tell the host we're ready
					ReadyManager.SendReadyStatusPacket(ClientReadyState.Ready);
				}
			}
			else if (Utils.IsInGame())
			{
				DebugConsole.Log("[GameClient] Client is in game - treating as reconnection");

				// We're in game already. Consider this a reconnection
				SetState(ClientState.InGame);

				// CRÍTICO: Habilitar processamento de pacotes
				PacketHandler.readyToProcess = true;
				DebugConsole.Log("[GameClient] PacketHandler.readyToProcess = true");

				if (IsHardSyncInProgress)
				{
					IsHardSyncInProgress = false;
					DebugConsole.Log("[GameClient] Cleared HardSyncInProgress flag");
				}

				Game.Instance?.Trigger(MP_HASHES.GameClient_OnConnectedInGame);
                ReadyManager.SendReadyStatusPacket(ClientReadyState.Ready);
				MultiplayerSession.CreateConnectedPlayerCursors();

				//CursorManager.Instance.AssignColor();
				SelectToolPatch.UpdateColor();

				// Fechar overlay se reconectou com sucesso
				MultiplayerOverlay.Close();

				DebugConsole.Log("[GameClient] Reconnection setup complete");
			}
			else
			{
				DebugConsole.LogWarning("[GameClient] Client is neither in menu nor in game - unexpected state");
			}
		}

		private static void OnDisconnected(string reason, CSteamID remote, ESteamNetworkingConnectionState state)
		{
			DebugConsole.LogWarning($"[GameClient] Connection closed or failed ({state}) for {remote}. Reason: {reason}");

			// If we're intentionally disconnecting for world loading, don't show error or return to title
			// We will reconnect automatically after the world finishes loading via ReconnectFromCache()
			if (_state == ClientState.LoadingWorld)
			{
				DebugConsole.Log("[GameClient] Ignoring disconnect callback - world is loading, will reconnect after.");
				return;
			}

			switch (state)
			{
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
					// The host closed our connection
					if (remote == MultiplayerSession.HostSteamID)
					{
						CoroutineRunner.RunOne(ShowMessageAndReturnToTitle());
					}
					break;
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
					// Something went wrong locally
					CoroutineRunner.RunOne(ShowMessageAndReturnToTitle());
					break;
			}
		}

		private static IEnumerator ShowMessageAndReturnToTitle()
		{
			MultiplayerOverlay.Show(STRINGS.UI.MP_OVERLAY.CLIENT.LOST_CONNECTION);
			//SaveHelper.CaptureWorldSnapshot();
			yield return new WaitForSeconds(3f);
			//PauseScreen.TriggerQuitGame(); // Force exit to frontend, getting a crash here
			if (Utils.IsInGame())
			{
				Utils.ForceQuitGame();
			}
			App.LoadScene("frontend");

			MultiplayerOverlay.Close();
			NetworkIdentityRegistry.Clear();
			SteamLobby.LeaveLobby();
		}

		#region Connection Health
		public static SteamNetConnectionRealTimeStatus_t? QueryConnectionHealth()
		{
			if (Connection.HasValue)
			{
				SteamNetConnectionRealTimeStatus_t status = default;
				SteamNetConnectionRealTimeLaneStatus_t laneStatus = default;

				EResult res = SteamNetworkingSockets.GetConnectionRealTimeStatus(
						Connection.Value,
						ref status,
						0,
						ref laneStatus
				);

				if (res == EResult.k_EResultOK)
				{
					return status;
				}
			}
			return null;
		}

		public static void EvaluateConnectionHealth()
		{
			connectionHealth = QueryConnectionHealth();
		}

		public static SteamNetConnectionRealTimeStatus_t? GetConnectionHealth()
		{
			return connectionHealth;
		}

		public static float GetLocalPacketQuality()
		{
			if (!connectionHealth.HasValue)
				return 0f;

			return connectionHealth.Value.m_flConnectionQualityLocal;
		}

		public static float GetRemotePacketQuality()
		{
			if (!connectionHealth.HasValue)
				return 0f;

			return connectionHealth.Value.m_flConnectionQualityRemote;
		}

		public static int GetPingToHost()
		{
			if (!connectionHealth.HasValue)
				return -1;

			return connectionHealth.Value.m_nPing;
		}

		public static int GetUnackedReliable()
		{
			if (!connectionHealth.HasValue)
				return -1;

			return connectionHealth.Value.m_cbSentUnackedReliable;
		}

		public static int GetPendingUnreliable()
		{
			if (!connectionHealth.HasValue)
				return -1;

			return connectionHealth.Value.m_cbPendingUnreliable;
		}

		public static long GetUsecQueueTime()
		{
			if (!connectionHealth.HasValue)
				return -1;

			return (long)connectionHealth.Value.m_usecQueueTime;
		}
		#endregion
		public static void CacheCurrentServer()
		{
			if (MultiplayerSession.HostSteamID != CSteamID.Nil)
			{
				_cachedConnectionInfo = new CachedConnectionInfo(
						MultiplayerSession.HostSteamID
				);
				DebugConsole.Log($"[GameClient] Cached server: {_cachedConnectionInfo.Value.HostSteamID}");
			}
			else
			{
				DebugConsole.LogWarning("[GameClient] Tried to cache, but HostSteamID is Nil.");
			}
		}

		public static void ReconnectFromCache()
		{
			if (_cachedConnectionInfo.HasValue)
			{
				DebugConsole.Log($"[GameClient] Reconnecting to cached server: {_cachedConnectionInfo.Value.HostSteamID}");
				var hostId = _cachedConnectionInfo.Value.HostSteamID;
				_cachedConnectionInfo = null; // Clear cache to prevent re-triggering
				ConnectToHost(hostId, false);
			}
			else
			{
				DebugConsole.LogWarning("[GameClient] No cached server info available to reconnect.");
			}
		}



		public static void PauseNetworkingCallbacks()
		{
			_pollingPaused = true;
			DebugConsole.Log("[GameClient] Networking callbacks paused.");
		}

		public static void ResumeNetworkingCallbacks()
		{
			_pollingPaused = false;
			DebugConsole.Log("[GameClient] Networking callbacks resumed.");
		}

		public static void OnModVerificationApproved()
		{
			DebugConsole.Log("[GameClient] Mod verification approved by host!");

			// DO NOT close overlay here - let connection flow manage it
			DebugConsole.Log("[GameClient] Mod verification approved, continuing connection flow");

			// Continue with normal connection flow
			ContinueConnectionFlow();
		}

		public static void DisableMessageHandlers()
		{
			if (_connectionStatusChangedCallback != null)
			{
				_connectionStatusChangedCallback.Unregister();
				_connectionStatusChangedCallback = null;
				DebugConsole.Log("[GameClient] Networking message handlers disabled.");
			}
		}

		public static void EnableMessageHandlers()
		{
			if (_connectionStatusChangedCallback == null)
			{
				_connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
				DebugConsole.Log("[GameClient] Networking message handlers enabled.");
			}
		}
	}
}
