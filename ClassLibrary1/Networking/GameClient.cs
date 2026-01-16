using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Handshake;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Profiling;
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

		private static ClientState _state = ClientState.Disconnected;
		public static ClientState State => _state;

		private static bool _pollingPaused = false;

		private static CachedConnectionInfo? _cachedConnectionInfo = null;

		public static bool IsHardSyncInProgress = false;
		private static bool _modVerificationSent = false;


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
			// I fucking hate this, maybe replace this with hashes?
			NetworkConfig.RelayClient.OnClientDisconnected = () => SetState(ClientState.Disconnected);
			NetworkConfig.RelayClient.OnClientConnected = () => SetState(ClientState.Connected);
			NetworkConfig.RelayClient.OnContinueConnectionFlow = () => ContinueConnectionFlow();
			NetworkConfig.RelayClient.OnReturnToMenu = () => CoroutineRunner.RunOne(ShowMessageAndReturnToTitle());
			NetworkConfig.RelayClient.OnRequestStateOrReturn = () =>
			{
                PacketSender.SendToHost(new GameStateRequestPacket(MultiplayerSession.LocalSteamID));
                MP_Timer.Instance.StartDelayedAction(10, () => CoroutineRunner.RunOne(ShowMessageAndReturnToTitle()));
            };
            NetworkConfig.RelayClient.Prepare();
		}

		public static void ConnectToHost(CSteamID hostSteamId, bool showLoadingScreen = true)
		{
			// Reset mod verification for new connection attempts
			_modVerificationSent = false;

			if (showLoadingScreen)
			{
				MultiplayerOverlay.Show(string.Format(STRINGS.UI.MP_OVERLAY.CLIENT.CONNECTING_TO_HOST, SteamFriends.GetFriendPersonaName(hostSteamId)));
			}

			SetState(ClientState.Connecting);
			NetworkConfig.RelayClient.ConnectToHost();
		}

		public static void Disconnect()
		{
			NetworkConfig.RelayClient.Disconnect();
		}

		public static void ReconnectToSession()
		{
			NetworkConfig.RelayClient.ReconnectToSession();
		}

		public static void Poll()
		{
			if (_pollingPaused)
				return;

			NetworkConfig.RelayClient.Update();

			switch (State)
			{
				case ClientState.Connected:
				case ClientState.InGame:
					NetworkConfig.RelayClient.OnMessageRecieved();
					break;
				case ClientState.Connecting:
				case ClientState.Disconnected:
				case ClientState.Error:
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
	}
}
