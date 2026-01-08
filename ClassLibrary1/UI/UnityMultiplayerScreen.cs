using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.UI.Components;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.lib.UIcmp;
using UnityEngine;
using static ONI_MP.STRINGS.UI.PAUSESCREEN;

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
		bool ShowMain, ShowLobbies, ShowHost;

		//Main Areas
		GameObject MainMenuSegment;
		GameObject StartHostingSegment;
		GameObject LobbyBrowserSegment;
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
		FInputField2 PasswortInput;
		FButton StartHosting, HostCancel;

		//LobbyBrowserSegment:
		FButton RefreshLobbiesBtn;
		FInputField2 LobbyFilter;
		GameObject LobbyListContainer;
		LobbyEntryUI LobbyEntryPrefab;
		Dictionary<LobbyListEntry, LobbyEntryUI> Lobbies = [];

		bool init = false;
		public void Init()
		{
			if (init) { return; }
			Debug.Log("Initializing MultiplayerScreen");
			MainMenuSegment = transform.Find("MainMenu").gameObject;
			StartHostingSegment = transform.Find("HostMenu").gameObject;
			LobbyBrowserSegment = transform.Find("LobbyList").gameObject;
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
			MainCancel = transform.Find("MainMenu/Cancel").gameObject.AddOrGet<FButton>();
			MainCancel.OnClick += () => Show(false);
			LobbyCodeInput = transform.Find("MainMenu/LobbyCodeJoin/Input").FindOrAddComponent<FInputField2>();
			LobbyCodeInput.Text = string.Empty;

			LobbyStateInfo = transform.Find("HostMenu/FriendsOnly/State").gameObject.GetComponent<LocText>();
			PrivateLobbyCheckbox = transform.Find("HostMenu/FriendsOnly/Checkbox").gameObject.AddOrGet<FToggle>();
			PrivateLobbyCheckbox.SetCheckmark("Checkmark");
			PrivateLobbyCheckbox.SetOnFromCode(true);
			TintLobbyState(true);
			PrivateLobbyCheckbox.OnChange += (on) => TintLobbyState(on);
			LobbySize = transform.Find("HostMenu/LobbySize/LobbySizeInput").gameObject.AddOrGet<FInputField2>();
			LobbySize.Text = "4";
			PasswortInput = transform.Find("HostMenu/PasswordInput").gameObject.AddOrGet<FInputField2>();
			PasswortInput.Text = string.Empty;

			StartHosting = transform.Find("HostMenu/StartHosting").gameObject.AddOrGet<FButton>();
			HostCancel = transform.Find("HostMenu/Cancel").gameObject.AddOrGet<FButton>();
			HostCancel.OnClick += () => ShowHostSegment(false);

			RefreshLobbiesBtn = transform.Find("LobbyList/SearchBar/RefreshButton").gameObject.AddOrGet<FButton>();
			RefreshLobbiesBtn.OnClick += () => RefreshLobbies();
			LobbyFilter = transform.Find("LobbyList/SearchBar/Input").gameObject.AddOrGet<FInputField2>();
			LobbyFilter.Text = string.Empty;
			LobbyListContainer = transform.Find("LobbyList/ScrollArea/Content").gameObject;

			var entryPrefabGO = transform.Find("LobbyList/ScrollArea/Content/EntryPrefab").gameObject;
			entryPrefabGO.SetActive(false);
			LobbyEntryPrefab = entryPrefabGO.AddOrGet<LobbyEntryUI>();

			init = true;
		}
		static string lastScene = string.Empty;
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
		Coroutine LobbyRefresh;

		public static void OpenFromMainMenu()
		{
			ShowWindow();
			Instance.ShowMainSegment(true);
			Instance.ShowHostSegment(false);
			Instance.ShowLobbySegment(false);
		}
		public static void OpenFromPauseScreen()
		{
			ShowWindow();
			Instance.ShowMainSegment(false);
			Instance.ShowHostSegment(true);
			Instance.ShowLobbySegment(false);
		}

		void ShowMainSegment(bool show)
		{
			ShowMain = show;
			MainMenuSegment.SetActive(show);
			RefreshSpacer();
		}
		void ShowHostSegment(bool show)
		{
			if (ShowLobbies)
				ShowLobbySegment(false);
			ShowHost = show;
			StartHostingSegment.SetActive(show);
			RefreshSpacer();
		}
		void ShowLobbySegment(bool show)
		{
			if (ShowHost)
				ShowHostSegment(false);
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
				//not yet handled
			}
			else
			{
				// Direct join
				SteamLobby.JoinLobby(lobby.LobbyId, (lobbyId) =>
				{
					DebugConsole.Log($"[LobbyBrowser] Successfully joined lobby: {lobbyId}");
					this.Show(false);
				});
			}
		}
	}
}
