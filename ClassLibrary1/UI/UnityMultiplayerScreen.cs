using ONI_MP.Networking;
using ONI_MP.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.lib.UIcmp;
using UnityEngine;
using static ONI_MP.MP_STRINGS.UI.PAUSESCREEN;

namespace ONI_MP.UI
{
	internal class UnityMultiplayerScreen : FScreen, IRender1000ms
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
		GameObject HostStartLobbySegment;
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
			HostStartLobbySegment = transform.Find("HostMenu").gameObject;
			LobbyBrowserSegment = transform.Find("LobbyList").gameObject;
			MiddleSpacer = transform.Find("MainSpacer").gameObject;

			CloseBtn = transform.Find("TopBar/CloseButton").gameObject.AddOrGet<FButton>();
			CloseBtn.OnClick += () => Show(false);

			HostGame = transform.Find("MainMenu/HostGameButton").gameObject.AddOrGet<FButton>();
			JoinViaSteam = transform.Find("MainMenu/JoinViaSteam").gameObject.AddOrGet<FButton>();
			OpenLobbyBrowser = transform.Find("MainMenu/OpenLobbyListButton").gameObject.AddOrGet<FButton>();
			JoinWithCode = transform.Find("MainMenu/LobbyCodeJoin/JoinWithCodeButton").gameObject.AddOrGet<FButton>();
			MainCancel = transform.Find("MainMenu/Cancel").gameObject.AddOrGet<FButton>();
			MainCancel.OnClick += () => Show(false);
			LobbyCodeInput = transform.Find("MainMenu/LobbyCodeJoin/Input").FindOrAddComponent<FInputField2>();
			LobbyCodeInput.Text = string.Empty;

			LobbyStateInfo = transform.Find("HostMenu/FriendsOnly/State").gameObject.GetComponent<LocText>();
			PrivateLobbyCheckbox = transform.Find("HostMenu/FriendsOnly/Checkbox").gameObject.AddOrGet<FToggle>();
			PrivateLobbyCheckbox.SetCheckmark("Checkmark");
			PrivateLobbyCheckbox.SetOnFromCode(true);
			LobbySize = transform.Find("HostMenu/LobbySize/LobbySizeInput").gameObject.AddOrGet<FInputField2>();
			LobbySize.Text = "4";
			PasswortInput = transform.Find("HostMenu/PasswordInput").gameObject.AddOrGet<FInputField2>();
			PasswortInput.Text = string.Empty;

			StartHosting = transform.Find("HostMenu/StartHosting").gameObject.AddOrGet<FButton>();
			HostCancel = transform.Find("HostMenu/Cancel").gameObject.AddOrGet<FButton>();

			RefreshLobbiesBtn = transform.Find("LobbyList/SearchBar/RefreshButton").gameObject.AddOrGet<FButton>();
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
			MainMenuSegment.SetActive(show);
			RefreshSpacer();
		}
		void ShowHostSegment(bool show)
		{
			HostStartLobbySegment.SetActive(show);
			RefreshSpacer();
		}
		void ShowLobbySegment(bool show)
		{
			LobbyBrowserSegment.SetActive(show);
			RefreshSpacer();
		}


		void RefreshSpacer()
		{
			MiddleSpacer.SetActive(ShowMain && (ShowLobbies || ShowHost));
		}

		int secondsPassed = 0;
		public void Render1000ms(float dt)
		{
			return;

			secondsPassed++;
			if (secondsPassed < 10)
				return;

			secondsPassed = 0;
			RefreshLobbies();
		}
		void RefreshLobbies()
		{
			if (!ShowLobbies)
				return;
		}


		LobbyEntryUI AddOrGetLobbyEntryUI(LobbyListEntry lobby)
		{
			if (Lobbies.TryGetValue(lobby, out LobbyEntryUI entryUI))
				return entryUI;

			entryUI = Util.KInstantiateUI<LobbyEntryUI>(LobbyEntryPrefab.gameObject, LobbyListContainer, true);
			entryUI.SetLobby(lobby);
			entryUI.SetJoinFunction(OnLobbyJoinClicked);
			Lobbies[lobby] = entryUI;
			return entryUI;
		}
		void OnLobbyJoinClicked(LobbyListEntry lobby)
		{

		}
	}
}
