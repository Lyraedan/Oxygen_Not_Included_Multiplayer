using ONI_MP.Networking;
using ONI_MP.Networking.States;
using Steamworks;
using System;
using Shared.Profiling;
using UnityEngine;

namespace ONI_MP.DebugTools
{
	public class DebugMenu : MonoBehaviour
	{
		private static DebugMenu _instance;

		private bool showMenu = false;
		private Rect windowRect = new Rect(10, 10, 250, 300); // Position and size
		private HierarchyViewer hierarchyViewer;
		private DebugConsole debugConsole;

		private Vector2 scrollPosition = Vector2.zero;


		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void Init()
		{
			Profiler.Scope();

			if (_instance != null) return;

			GameObject go = new GameObject("ONI_MP_DebugMenu");
			DontDestroyOnLoad(go);
			_instance = go.AddComponent<DebugMenu>();
		}

		private void Awake()
		{
			Profiler.Scope();

			hierarchyViewer = gameObject.AddComponent<HierarchyViewer>();
			//debugConsole = gameObject.AddComponent<DebugConsole>();
		}

		private void Update()
		{
			//if (Input.GetKeyDown(KeyCode.F2) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
			//{
			//	showMenu = !showMenu;
			//}
		}

		private void OnGUI()
		{
			Profiler.Scope();

			if (!showMenu) return;

			GUIStyle windowStyle = new GUIStyle(GUI.skin.window) { padding = new RectOffset(10, 10, 20, 20) };
			windowRect = GUI.ModalWindow(888, windowRect, DrawMenuContents, "DEBUG MENU", windowStyle);
		}

		private void DrawMenuContents(int windowID)
		{
			Profiler.Scope();

			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.Width(windowRect.width - 20), GUILayout.Height(windowRect.height - 40));

			if (GUILayout.Button("Toggle Hierarchy Viewer"))
				hierarchyViewer.Toggle();

			if (GUILayout.Button("Send Unready Packet"))
                ReadyManager.SendReadyStatusPacket(ClientReadyState.Unready);

            if (GUILayout.Button("Send Ready Packet"))
                ReadyManager.SendReadyStatusPacket(ClientReadyState.Ready);

			GUILayout.EndScrollView();

			GUI.DragWindow();
		}

		private void DrawPlayerList()
		{
			Profiler.Scope();

			GUILayout.Label("Players in Lobby:", UnityEngine.GUI.skin.label);

			var players = SteamLobby.GetAllLobbyMembers();
			if (players.Count == 0)
			{
				GUILayout.Label("<none>", UnityEngine.GUI.skin.label);
			}
			else
			{
				foreach (CSteamID playerId in players)
				{
					var playerName = SteamFriends.GetFriendPersonaName(playerId);
					string prefix = (MultiplayerSession.HostSteamID == playerId) ? "[HOST] " : "";
					GUILayout.Label($"{prefix}{playerName} ({playerId})", UnityEngine.GUI.skin.label);
				}
			}
		}


	}
}
