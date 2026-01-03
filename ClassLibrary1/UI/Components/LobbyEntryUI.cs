using ONI_MP.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.lib.UIcmp;

namespace ONI_MP.UI.Components
{
	internal class LobbyEntryUI : KMonoBehaviour
	{
		LocText WorldName, Host, Players, Cycle, Dupes, Ping;
		FButton JoinButton;
		LobbyListEntry Lobby;
		System.Action<LobbyListEntry> OnJoinClicked = null;


		public override void OnPrefabInit()
		{
			base.OnPrefabInit();
			WorldName = transform.Find("World").gameObject.GetComponent<LocText>();
			Host = transform.Find("Host").gameObject.GetComponent<LocText>();
			Players = transform.Find("Players").gameObject.GetComponent<LocText>();
			Cycle = transform.Find("Cycle").gameObject.GetComponent<LocText>();
			Dupes = transform.Find("Dupes").gameObject.GetComponent<LocText>();
			Ping = transform.Find("Ping").gameObject.GetComponent<LocText>();
			JoinButton = transform.Find("JoinButton").gameObject.AddOrGet<FButton>();
			JoinButton.OnClick += JoinLobbyClicked;
		}
		void JoinLobbyClicked()
		{
			if (OnJoinClicked != null && Lobby != null)
				OnJoinClicked(Lobby);
		}

		public void SetLobby(LobbyListEntry _lobby)
		{
			Lobby = _lobby;
			RefreshDisplayedInfo();
		}
		public void SetJoinFunction(System.Action<LobbyListEntry> onJoin) => OnJoinClicked = onJoin;

		public void RefreshDisplayedInfo()
		{
			if (Lobby == null)
				return;
			WorldName.SetText(Lobby.ColonyDisplay);
			Host.SetText(Lobby.HostDisplayWithBadge);
			Players.SetText(Lobby.PlayerCountDisplay);
			Cycle.SetText(Lobby.CycleDisplay);
			Dupes.SetText(Lobby.DuplicantDisplay);
			Ping.SetText(Lobby.PingDisplay);
		}
	}
}
