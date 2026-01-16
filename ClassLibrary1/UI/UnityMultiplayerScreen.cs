using NodeEditorFramework;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.UI.Components;
using ONI_MP.UI.lib;
using Shared.Helpers;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.lib.UIcmp;
using UnityEngine;
using static ONI_MP.STRINGS.UI;
using static ONI_MP.STRINGS.UI.MP_SCREEN.HOSTMENU;
using static ONI_MP.STRINGS.UI.MP_SCREEN.HOSTMENU.LOBBYSIZE;
using static ONI_MP.STRINGS.UI.PAUSESCREEN;
using static ONI_MP.UI.UnityMultiplayerScreen;

namespace ONI_MP.UI
{
	internal class UnityMultiplayerScreen : FScreen
	{
		public static void OnSceneChanged()
		{
			if (Instance != null)
			{
				UnityEngine.Object.Destroy(Instance.gameObject);
				Instance = null;
			}
		}

		public static UnityMultiplayerScreen Instance;
		bool ShowMain, ShowLobbies, ShowHost, ShowAdditionalHostSettings;

		//Main Areas
		GameObject MainMenuSegment;
		GameObject StartHostingSegment;
		GameObject LobbyBrowserSegment;
		GameObject AdditionalHostSettingsSegment;
		GameObject MiddleSpacer;
		FButton CloseBtn;


		//MainMenuSegment:
		FButton
			HostGame,
			JoinViaSteam,
			OpenLobbyBrowser,
			JoinWithCode,
			MainCancel;
		FInputField2 LobbyCodeInput;

		//HostStartLobbySegment:
		FToggle PrivateLobbyCheckbox;
		LocText LobbyStateInfo;
		FInputField2 LobbySize;
		FButton IncreaseSize, DecreaseSize;
		FInputField2 PasswortInput;
		FButton AdditionalLobbySettings;
		FButton StartHosting, HostCancel;

		//LobbyBrowserSegment:
		FButton RefreshLobbiesBtn;
		FInputField2 LobbyFilter;
		GameObject LobbyListContainer;
		LobbyEntryUI LobbyEntryPrefab;
		Dictionary<LobbyListEntry, LobbyEntryUI> Lobbies = [];

		Callback<LobbyDataUpdate_t> lobbyDataCallback;

		bool init = false;
		static string lastScene = string.Empty;
		Coroutine LobbyRefresh;
		ulong _pendingLobbyId = Utils.NilUlong();

		public void Init()
		{
			if (init) { return; }

			lobbyDataCallback = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdateReceived);

			Debug.Log("Initializing MultiplayerScreen");
			MainMenuSegment = transform.Find("MainMenu").gameObject;
			StartHostingSegment = transform.Find("HostMenu").gameObject;
			LobbyBrowserSegment = transform.Find("LobbyList").gameObject;
			AdditionalHostSettingsSegment = transform.Find("AdditionalHostSettings").gameObject;
			MiddleSpacer = transform.Find("MainSpacer").gameObject;
			CloseBtn = transform.Find("TopBar/CloseButton").gameObject.AddOrGet<FButton>();
			CloseBtn.OnClick += () => Show(false);

			HostGame = transform.Find("MainMenu/HostGameButton").gameObject.AddOrGet<FButton>();
			HostGame.OnClick += () => ShowHostSegment(true);
			JoinViaSteam = transform.Find("MainMenu/JoinViaSteam").gameObject.AddOrGet<FButton>();
			JoinViaSteam.OnClick += () => SteamFriends.ActivateGameOverlay("friends");
			OpenLobbyBrowser = transform.Find("MainMenu/OpenLobbyListButton").gameObject.AddOrGet<FButton>();
			OpenLobbyBrowser.OnClick += () => ShowLobbySegment(true);

			JoinWithCode = transform.Find("MainMenu/LobbyCodeJoin/JoinWithCodeButton").gameObject.AddOrGet<FButton>();
			JoinWithCode.OnClick += JoinLobbyWithCode;
			MainCancel = transform.Find("MainMenu/Cancel").gameObject.AddOrGet<FButton>();
			MainCancel.OnClick += () => Show(false);
			LobbyCodeInput = transform.Find("MainMenu/LobbyCodeJoin/Input").FindOrAddComponent<FInputField2>();
			LobbyCodeInput.Text = string.Empty;
			LobbyCodeInput.inputField.characterLimit = 16;

			LobbyStateInfo = transform.Find("HostMenu/FriendsOnly/State").gameObject.GetComponent<LocText>();
			PrivateLobbyCheckbox = transform.Find("HostMenu/FriendsOnly/Checkbox").gameObject.AddOrGet<FToggle>();
			PrivateLobbyCheckbox.SetCheckmark("Checkmark");
			PrivateLobbyCheckbox.SetOnFromCode(true);
			TintLobbyState(true);
			PrivateLobbyCheckbox.OnChange += (on) => TintLobbyState(on);
			LobbySize = transform.Find("HostMenu/LobbySize/LobbySizeInput").gameObject.AddOrGet<FInputField2>();
			LobbySize.Text = SteamLobby.LOBBY_SIZE_DEFAULT.ToString();
			LobbySize.OnValueChanged.AddListener(ClampLobbySize);

			IncreaseSize = transform.Find("HostMenu/LobbySize/LobbySizeInput/Increase").gameObject.AddOrGet<FButton>();
			IncreaseSize.OnClick += IncreaseLobbySize;
			DecreaseSize = transform.Find("HostMenu/LobbySize/LobbySizeInput/Decrease").gameObject.AddOrGet<FButton>();
			DecreaseSize.OnClick += DecreaseLobbySize;


			PasswortInput = transform.Find("HostMenu/PasswordInput").gameObject.AddOrGet<FInputField2>();
			PasswortInput.Text = string.Empty;

			AdditionalLobbySettings = transform.Find("HostMenu/AdditionalSettings").gameObject.AddOrGet<FButton>();
			AdditionalLobbySettings.SetInteractable(false);
			UIUtils.AddSimpleTooltipToObject(AdditionalLobbySettings.gameObject, WORK_IN_PROGRESS);

			StartHosting = transform.Find("HostMenu/Buttons/StartHosting").gameObject.AddOrGet<FButton>();
			StartHosting.OnClick += () => StartHostingGame();
			HostCancel = transform.Find("HostMenu/Buttons/Cancel").gameObject.AddOrGet<FButton>();
			HostCancel.OnClick += () => CancelHosting();

			RefreshLobbiesBtn = transform.Find("LobbyList/SearchBar/RefreshButton").gameObject.AddOrGet<FButton>();
			RefreshLobbiesBtn.OnClick += () => RefreshLobbies();
			LobbyFilter = transform.Find("LobbyList/SearchBar/Input").gameObject.AddOrGet<FInputField2>();
			LobbyFilter.Text = string.Empty;
			LobbyListContainer = transform.Find("LobbyList/ScrollArea/Content").gameObject;

			var entryPrefabGO = transform.Find("LobbyList/ScrollArea/Content/EntryPrefab").gameObject;
			entryPrefabGO.SetActive(false);
			LobbyEntryPrefab = entryPrefabGO.AddOrGet<LobbyEntryUI>();
			RefreshLobbySizeButtons();
			init = true;
		}

		void IncreaseLobbySize()
		{
			if (int.TryParse(LobbySize.Text, out int lobbySize))
			{
				lobbySize++;
				lobbySize = Mathf.Clamp(lobbySize, SteamLobby.LOBBY_SIZE_MIN, SteamLobby.LOBBY_SIZE_MAX);
				LobbySize.SetTextFromData(lobbySize.ToString());
				RefreshLobbySizeButtons();
			}
		}

		void DecreaseLobbySize()
		{
			if (int.TryParse(LobbySize.Text, out int lobbySize))
			{
				lobbySize--;
				lobbySize = Mathf.Clamp(lobbySize, SteamLobby.LOBBY_SIZE_MIN, SteamLobby.LOBBY_SIZE_MAX);
				LobbySize.SetTextFromData(lobbySize.ToString());
				RefreshLobbySizeButtons();
			}
		}
		void RefreshLobbySizeButtons()
		{
			if (!int.TryParse(LobbySize.Text, out int lobbySize))
				lobbySize = SteamLobby.LOBBY_SIZE_DEFAULT;

			IncreaseSize.SetInteractable(lobbySize < SteamLobby.LOBBY_SIZE_MAX);
			DecreaseSize.SetInteractable(lobbySize > SteamLobby.LOBBY_SIZE_MIN);
		}
		void CancelHosting()
		{
			if (ShowMain)
				ShowHostSegment(false);
			else
				Show(false);
		}
		void ClampLobbySize(string text)
		{
			if (int.TryParse(text, out int lobbySize))
			{
				lobbySize = Mathf.Clamp(lobbySize, SteamLobby.LOBBY_SIZE_MIN, SteamLobby.LOBBY_SIZE_MAX);
				LobbySize.SetTextFromData(lobbySize.ToString());
			}
			else
				LobbySize.SetTextFromData(SteamLobby.LOBBY_SIZE_DEFAULT.ToString());
			RefreshLobbySizeButtons();
		}

		public static void ShowWindow()
		{
			string currentScene = App.GetCurrentSceneName();
			if (currentScene != lastScene)
				OnSceneChanged();
			lastScene = currentScene;
			if (Instance == null)
			{
				var screen = Util.KInstantiateUI(ModAssets.MP_ScreenPrefab, ModAssets.ParentScreen, true);
				Instance = screen.AddOrGet<UnityMultiplayerScreen>();
				Instance.Init();
			}
			Instance.Show(true);
			Instance.ConsumeMouseScroll = true;
			Instance.transform.SetAsLastSibling();
		}
		public override void OnShow(bool show)
		{
			base.OnShow(show);

			if (show)
				LobbyRefresh = StartCoroutine(RefreshLobbiesEnumerator());
			else
				StopCoroutine(LobbyRefresh);
		}

		public static void OpenFromMainMenu()
		{
			ShowWindow();
			Instance.ShowMainSegment(true);
			Instance.ShowHostSegment(false);
			Instance.ShowLobbySegment(false);
			Instance.ShowAdditionalHostSettingsSegment(false);
		}
		public static void OpenFromPauseScreen()
		{
			ShowWindow();
			Instance.ShowMainSegment(false);
			Instance.ShowLobbySegment(false);
			Instance.ShowHostSegment(true);
			Instance.ShowAdditionalHostSettingsSegment(false);
		}
		void JoinLobbyWithCode()
		{
			// First step: Validate and parse code
			string code = LobbyCodeHelper.CleanCode(LobbyCodeInput.Text);

			if (string.IsNullOrEmpty(code))
			{
				DialogUtil.CreateConfirmDialogFrontend(JOINBYDIALOGMENU.JOIN_BY_CODE, STRINGS.UI.JOINBYDIALOGMENU.ERR_ENTER_CODE);
				return;
			}

			if (!LobbyCodeHelper.IsValidCodeFormat(code))
			{
				DialogUtil.CreateConfirmDialogFrontend(JOINBYDIALOGMENU.JOIN_BY_CODE, STRINGS.UI.JOINBYDIALOGMENU.ERR_INVALID_CODE);
				return;
			}

			if (!LobbyCodeHelper.TryParseCode(code, out ulong lobbyId))
			{
				DialogUtil.CreateConfirmDialogFrontend(JOINBYDIALOGMENU.JOIN_BY_CODE, STRINGS.UI.JOINBYDIALOGMENU.ERR_PARSE_CODE_FAILED);
				return;
			}

			_pendingLobbyId = lobbyId;

			// We need to join the lobby to get its metadata (including password status)
			// But first, let's check if we can get the data by requesting lobby data
			SteamMatchmaking.RequestLobbyData(lobbyId.AsCSteamID());
		}

		void OnLobbyDataUpdateReceived(LobbyDataUpdate_t data)
		{
			if (data.m_ulSteamIDLobby != _pendingLobbyId)
				return;

			if (data.m_bSuccess == 0)
				return;

			JoinOrOpenPasswordDialogue(_pendingLobbyId);
			_pendingLobbyId = Utils.NilUlong();
		}

		void JoinOrOpenPasswordDialogue(ulong lobbyId)
		{
			bool hasPassword = SteamMatchmaking.GetLobbyData(lobbyId.AsCSteamID(), "has_password") == "1";

			if (!hasPassword)
				JoinSteamLobby(lobbyId);
			else
				OpenPasswordDialogue(lobbyId);

		}
		void JoinSteamLobby(ulong lobbyId)
		{
			SteamLobby.JoinLobby(lobbyId.AsCSteamID(), (lobbyId) =>
			{
				DebugConsole.Log($"[LobbyBrowser] Successfully joined lobby: {lobbyId}");
				this.Show(false);
			});
		}
		void OpenPasswordDialogue(ulong lobbyId)
		{
			UnityPasswordInputDialogueUI.ShowPasswordDialogueFor(lobbyId);
		}


		void ShowMainSegment(bool show)
		{
			ShowMain = show;
			MainMenuSegment.SetActive(show);
			RefreshSpacer();
		}
		void ShowHostSegment(bool show)
		{
			if (ShowLobbies && show)
				ShowLobbySegment(false);
			ShowHost = show;
			StartHostingSegment.SetActive(show);
			if (ShowAdditionalHostSettings && !show)
				ShowAdditionalHostSettingsSegment(false);
			RefreshSpacer();
		}
		void ShowAdditionalHostSettingsSegment(bool show)
		{
			ShowAdditionalHostSettings = show;
			AdditionalHostSettingsSegment.SetActive(show);
		}
		void ShowLobbySegment(bool show)
		{
			if (ShowHost && show)
			{
				ShowHostSegment(false);
			}
			ShowLobbies = show;
			LobbyBrowserSegment.SetActive(show);
			RefreshSpacer();
			if (show)
				RefreshLobbies();
		}

		static Color PublicLobbyTint = new Color(0.4f, 1f, 0.6f), PrivateLobbyTint = new Color(1f, 0.8f, 0.4f);

		void TintLobbyState(bool isPrivate)
		{
			string text = isPrivate ? STRINGS.UI.MP_SCREEN.HOSTMENU.FRIENDSONLY.LOBBY_VISIBILITY_FRIENDSONLY : STRINGS.UI.MP_SCREEN.HOSTMENU.FRIENDSONLY.LOBBY_VISIBILITY_PUBLIC;
			LobbyStateInfo.SetText(Utils.ColorText(text, isPrivate ? PrivateLobbyTint : PublicLobbyTint));
		}


		void RefreshSpacer()
		{
			MiddleSpacer.SetActive(ShowMain && (ShowLobbies || ShowHost));
		}

		int secondsPassed = 0;
		IEnumerator RefreshLobbiesEnumerator()
		{
			for (; ; )
			{
				RefreshLobbies();
				yield return new WaitForSeconds(10);
			}
		}

		void RefreshLobbies()
		{
			if (!ShowLobbies)
				return;

			SteamLobby.RequestLobbyList(OnLobbyListReceived);
		}
		private void OnLobbyListReceived(List<LobbyListEntry> lobbies)
		{
			foreach (var existing in Lobbies.Values)
			{
				existing.Hide();
			}
			foreach (var current in lobbies)
			{
				var entry = AddOrGetLobbyEntryUI(current);
				entry.RefreshDisplayedInfo();
			}
		}

		LobbyEntryUI AddOrGetLobbyEntryUI(LobbyListEntry lobby)
		{
			if (Lobbies.TryGetValue(lobby, out LobbyEntryUI entryUI))
			{
				entryUI.gameObject.SetActive(true);
				return entryUI;
			}
			entryUI = Util.KInstantiateUI<LobbyEntryUI>(LobbyEntryPrefab.gameObject, LobbyListContainer);
			entryUI.gameObject.SetActive(true);
			entryUI.SetLobby(lobby);
			entryUI.SetJoinFunction(OnLobbyJoinClicked);
			Lobbies[lobby] = entryUI;
			return entryUI;
		}
		void OnLobbyJoinClicked(LobbyListEntry lobby)
		{
			if (lobby.HasPassword)
			{
				OpenPasswordDialogue(lobby.LobbyId);
			}
			else
			{
				// Direct join
				JoinSteamLobby(lobby.LobbyId);
			}
		}

		void StoreHostConfigurationSettings()
		{
			Configuration.Instance.Host.Lobby.IsPrivate = PrivateLobbyCheckbox.On;
			string lobbySize = LobbySize.Text;
			if (lobbySize.Any())
			{
				if (!int.TryParse(lobbySize, out int maxLobbySize))
					maxLobbySize = SteamLobby.LOBBY_SIZE_MIN;
				maxLobbySize = Mathf.Clamp(maxLobbySize, SteamLobby.LOBBY_SIZE_MIN, SteamLobby.LOBBY_SIZE_MAX);
				Configuration.Instance.Host.MaxLobbySize = maxLobbySize;
			}
			else
			{
				Configuration.Instance.Host.MaxLobbySize = SteamLobby.LOBBY_SIZE_DEFAULT;
			}

			string password = PasswortInput.Text;
			if (password.Any())
			{
				Configuration.Instance.Host.Lobby.RequirePassword = true;
				Configuration.Instance.Host.Lobby.PasswordHash = PasswordHelper.HashPassword(password);
			}
			else
			{
				Configuration.Instance.Host.Lobby.RequirePassword = false;
				Configuration.Instance.Host.Lobby.PasswordHash = string.Empty;
			}
			Configuration.Instance.Save();
		}

		private void StartHostingGame()
		{
			// Save the host config
			StoreHostConfigurationSettings();

			if (Utils.IsInGame())
			{
				SteamLobby.CreateLobby(onSuccess: () =>
				{
					//SpeedControlScreen.Instance?.Unpause(false);
					Game.Instance.Trigger(MP_HASHES.OnMultiplayerGameSessionInitialized);
				});
			}
			else
			{
				MultiplayerSession.ShouldHostAfterLoad = true; // Set flag to start hosting after loading

				string saveForCurrentDlc = SaveLoader.GetLatestSaveForCurrentDLC();
				bool hasVersionCompatibleSave = !string.IsNullOrEmpty(saveForCurrentDlc) && System.IO.File.Exists(saveForCurrentDlc);
				if (hasVersionCompatibleSave)
				{
					DebugConsole.Log($"[UnityMultiplayerScreen/StartHostingGame] Found existing compatible savefile. Opening load sequence");
					MainMenu.Instance?.LoadGame();
					RegisterOnExitLoadScreenTriggers();
				}
				else
				{
					DebugConsole.Log("$[UnityMultiplayerScreen/StartHostingGame] No saves found! Running new game sequence.");
					MainMenu.Instance?.NewGame();
				}
			}
			Show(false);
		}
		void RegisterOnExitLoadScreenTriggers()
		{
			LoadScreen.Instance.closeButton.onClick += OnLoadScreenExited;
			UI_Patches.OnLoadScreenExited = OnLoadScreenExited;
		}
		void OnLoadScreenExited()
		{
			UI_Patches.OnLoadScreenExited = null;
			LoadScreen.Instance.closeButton.onClick -= OnLoadScreenExited;
			MultiplayerSession.ShouldHostAfterLoad = false; // Reset the flag if the load screen is closed
			OpenFromMainMenu();
		}
	}
}

