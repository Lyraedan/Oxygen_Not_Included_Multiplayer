using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Packets.Handshake;
using ONI_MP.Networking.Compatibility;
using ONI_MP.Networking.States;
using ONI_MP.Patches.ToolPatches;
using Steamworks;
using System;
using System.Collections;
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
				MultiplayerOverlay.Show(string.Format(MP_STRINGS.UI.MP_OVERLAY.CLIENT.CONNECTING_TO_HOST, SteamFriends.GetFriendPersonaName(hostSteamId)));
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

			if (msgCount > 0)
			{
				DebugConsole.Log($"[GameClient] ProcessIncomingMessages() - Received {msgCount} messages");
			}

			for (int i = 0; i < msgCount; i++)
			{
				var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(messages[i]);
				byte[] data = new byte[msg.m_cbSize];
				Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);

				try
				{
					DebugConsole.Log($"[GameClient] Processing packet {i+1}/{msgCount}, size: {msg.m_cbSize} bytes, readyToProcess: {PacketHandler.readyToProcess}");
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

		private static void OnConnected()
		{
			//MultiplayerOverlay.Close();
			SetState(ClientState.Connected);

			// We've reconnected in game
			MultiplayerSession.InSession = true;

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
				DebugConsole.Log("[GameClient] Skipping mod verification - we are the host");
				ContinueConnectionFlow();
				return;
			}

			// Reset mod verification state on new connection
			_modVerificationSent = false;

			// CRITICAL: Enable packet processing BEFORE mod verification
			// Otherwise, the mod verification response will be discarded!
			PacketHandler.readyToProcess = true;
			DebugConsole.Log("[GameClient] PacketHandler.readyToProcess = true (before mod verification)");

			// First step: Send mod verification packet to host (CLIENTS ONLY)
			if (!_modVerificationSent)
			{
				DebugConsole.Log("[GameClient] Sending mod verification to host...");
				// Overlay removed at user's request - verification happens silently

				try
				{
					var modVerificationPacket = new ModVerificationPacket(MultiplayerSession.LocalSteamID);
					PacketSender.SendToHost(modVerificationPacket);
					_modVerificationSent = true;
					DebugConsole.Log("[GameClient] Mod verification packet sent successfully. Waiting for response...");
				}
				catch (System.Exception ex)
				{
					DebugConsole.LogWarning($"[GameClient] Failed to send mod verification: {ex.Message}");
					MultiplayerOverlay.Close();
					return;
				}

				// Wait for host response before proceeding
				return;
			}

			DebugConsole.LogWarning("[GameClient] Mod verification was sent but no response received yet. Still waiting...");
			// We should only reach here if verification was sent but no response received yet

			// Continue with normal connection flow only if mod verification passed
			ContinueConnectionFlow();
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
				MultiplayerOverlay.Show(string.Format(MP_STRINGS.UI.MP_OVERLAY.CLIENT.WAITING_FOR_PLAYER, SteamFriends.GetFriendPersonaName(MultiplayerSession.HostSteamID)));
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

			switch(state)
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
			MultiplayerOverlay.Show(MP_STRINGS.UI.MP_OVERLAY.CLIENT.LOST_CONNECTION);
            //SaveHelper.CaptureWorldSnapshot();
            yield return new WaitForSeconds(3f);
            //PauseScreen.TriggerQuitGame(); // Force exit to frontend, getting a crash here

            Game.Instance.SetIsLoading();
            Grid.CellCount = 0;
            Sim.Shutdown();
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

        public static bool HasPacketLoss()
        {
            if (!connectionHealth.HasValue)
                return false;

			float localQuality = GetLocalPacketQuality();
            return localQuality < 0.7f;
        }

        public static bool HasReliablePacketLoss()
        {
            if (!connectionHealth.HasValue)
                return false;

            return connectionHealth.Value.m_cbSentUnackedReliable > 0;
        }

        public static bool HasSevereReliableLoss()
        {
            if (!connectionHealth.HasValue)
                return false;

            return connectionHealth.Value.m_cbSentUnackedReliable > 32 * 1024; // 32 KB backlog
        }

        public static bool HasUnreliablePacketLoss()
        {
            if (!connectionHealth.HasValue)
                return false;

            return connectionHealth.Value.m_cbPendingUnreliable > 0;
        }

        public static bool HasNetworkJitter()
        {
            if (!connectionHealth.HasValue)
                return false;

            // > 50ms queued
            return (long) connectionHealth.Value.m_usecQueueTime > 50_000;
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

			return (long) connectionHealth.Value.m_usecQueueTime;
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

		public static void OnModVerificationRejected(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches, ulong[] steamModIds)
		{
			DebugConsole.Log($"[GameClient] Mod verification REJECTED by host: {reason}");
			DebugConsole.Log($"[GameClient] Steam mods available for auto-install: {steamModIds?.Length ?? 0}");
			DebugConsole.Log("[GameClient] Disconnecting client due to mod incompatibility...");

			// Show detailed error to user with option to install mods
			ShowModIncompatibilityError(reason, missingMods, extraMods, versionMismatches, steamModIds);

			// Disconnect from host immediately
			Disconnect();

			DebugConsole.Log("[GameClient] Client disconnected successfully due to mod incompatibility");
		}

		private static void ShowModIncompatibilityError(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches, ulong[] steamModIds)
		{
			try
			{
				// DO NOT close MultiplayerOverlay here - we need it to show the error message
				// MultiplayerOverlay.Close(); // REMOVED - was causing popup to disappear

				// Build detailed error message for console log
				var errorMessage = $"Mod compatibility check failed:\n{reason}\n\n";

				if (missingMods != null && missingMods.Length > 0)
				{
					errorMessage += $"Missing mods (install these):\n";
					foreach (var mod in missingMods)
					{
						errorMessage += $"• {mod}\n";
					}
					errorMessage += "\n";
				}

				if (extraMods != null && extraMods.Length > 0)
				{
					errorMessage += $"Extra mods (disable these):\n";
					foreach (var mod in extraMods)
					{
						errorMessage += $"• {mod}\n";
					}
					errorMessage += "\n";
				}

				if (versionMismatches != null && versionMismatches.Length > 0)
				{
					errorMessage += $"Version mismatches (update these):\n";
					foreach (var mod in versionMismatches)
					{
						errorMessage += $"• {mod}\n";
					}
					errorMessage += "\n";
				}

				errorMessage += "Please ensure your mods match the host's configuration.";

				// Log error to console
				DebugConsole.Log($"[GameClient] {errorMessage}");

				// Show UI popup with mod compatibility details with auto-install option - this will keep overlay visible
				ModCompatibilityPopup.ShowIncompatibilityError(reason, missingMods, extraMods, versionMismatches, steamModIds);
			}
			catch (Exception ex)
			{
				DebugConsole.LogWarning($"[GameClient] Error showing mod incompatibility dialog: {ex.Message}");
			}
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
