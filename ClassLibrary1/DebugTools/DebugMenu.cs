using ONI_MP.Networking;
using ONI_MP.Networking.States;
using ONI_MP.Networking.Transport.Steamworks;
using ONI_MP.Patches.ToolPatches;
using Steamworks;
using System;
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

		// LAN
        private string lanHostIP = "127.0.0.1";
        private string lanHostPort = "7777";
        private string[] hostTransportOptions = new string[]
        {
            "Steam",
            "LAN"
        };
        private int selectedHostTransport = 0;

        private string lanJoinAddress = "127.0.0.1:7777";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void Init()
		{
			if (_instance != null) return;

			GameObject go = new GameObject("ONI_MP_DebugMenu");
			DontDestroyOnLoad(go);
			_instance = go.AddComponent<DebugMenu>();
		}

		private void Awake()
		{
			hierarchyViewer = gameObject.AddComponent<HierarchyViewer>();
			//debugConsole = gameObject.AddComponent<DebugConsole>();
		}

		private void Update()
		{
            if (Input.GetKeyDown(KeyCode.F2) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
			{
				showMenu = !showMenu;
			}
		}

		private void OnGUI()
		{
			if (!showMenu) return;

			GUIStyle windowStyle = new GUIStyle(GUI.skin.window) { padding = new RectOffset(10, 10, 20, 20) };
			windowRect = GUI.ModalWindow(888, windowRect, DrawMenuContents, "DEBUG MENU", windowStyle);
		}

        private void DrawMenuContents(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                false,
                true,
                GUILayout.Width(windowRect.width - 20),
                GUILayout.Height(windowRect.height - 40)
            );

            GUILayout.Label("Hosting", GUI.skin.box);
            GUILayout.Label("Transport:");
            selectedHostTransport = GUILayout.Toolbar(selectedHostTransport, hostTransportOptions);
            DebugConsole.Log($"Selected Host Transport: {hostTransportOptions[selectedHostTransport]} : {selectedHostTransport}");

            GUILayout.Space(5);

            GUILayout.Label("Host IP:");
            lanHostIP = GUILayout.TextField(lanHostIP);

            GUILayout.Label("Port:");
            lanHostPort = GUILayout.TextField(lanHostPort);

            if (GUILayout.Button("Start Hosting"))
            {
                if(selectedHostTransport == 0)
                {
                    NetworkConfig.NetworkTransport selected_transport = NetworkConfig.NetworkTransport.STEAMWORKS;
                    Configuration.Instance.Host.NetworkTransport = (int)selected_transport;
                    NetworkConfig.UpdateTransport(selected_transport);
                    Configuration.Instance.Save();

                    SteamLobby.CreateLobby(onSuccess: () =>
                    {
                        SpeedControlScreen.Instance?.Unpause(false);
                        Game.Instance.Trigger(MP_HASHES.OnMultiplayerGameSessionInitialized);
                    });
                    return;
                }

                if (int.TryParse(lanHostPort, out int port))
                {
                    DebugConsole.Log($"[LAN] Hosting on {lanHostIP}:{port}");

                    Configuration.Instance.Host.LanSettings.Ip = lanHostIP;
                    Configuration.Instance.Host.LanSettings.Port = port;

                    NetworkConfig.NetworkTransport selected_transport = NetworkConfig.NetworkTransport.RIPTIDE;
                    Configuration.Instance.Host.NetworkTransport = (int)selected_transport;
                    NetworkConfig.UpdateTransport(selected_transport);

                    Configuration.Instance.Save();

                    StartServer();
                }
                else
                {
                    DebugConsole.LogError("Invalid port!");
                }
            }
            if (GUILayout.Button("Stop Hosting"))
            {
                if(selectedHostTransport == 0)
                {
                    SteamLobby.LeaveLobby();
                    return;
                }

                Stop();
            }


            GUILayout.Space(10);

            GUILayout.Label("LAN Join", GUI.skin.box);

            GUILayout.Label("Server Address:");
            lanJoinAddress = GUILayout.TextField(lanJoinAddress);

            if (GUILayout.Button("Join Server"))
            {
                DebugConsole.Log($"[LAN] Joining {lanJoinAddress}");

                string[] address = lanJoinAddress.Split(':');
                if(address.Length != 2)
                {
                    DebugConsole.LogError("Invalid address format! Use IP:Port");
                    return;
                }

                if (int.TryParse(address[1], out int port))
                {
                    Configuration.Instance.Client.LanSettings.Ip = address[0];
                    Configuration.Instance.Client.LanSettings.Port = port;
                    Configuration.Instance.Save();

                    Join();
                }
            }

            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        void StartServer()
        {
            MultiplayerSession.Clear();
            try
            {
                DebugConsole.Log("Starting GameServer...");
                Networking.GameServer.Start();
                DebugConsole.Log("GameServer started successfully.");
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"GameServer.Start() failed: {ex}");
            }
            SelectToolPatch.UpdateColor();
            Game.Instance.Trigger(MP_HASHES.OnMultiplayerGameSessionInitialized);
        }

        void Stop()
        {
            if (MultiplayerSession.IsHost)
                Networking.GameServer.Shutdown();

            if (MultiplayerSession.IsClient)
                GameClient.Disconnect();

            NetworkIdentityRegistry.Clear();
            MultiplayerSession.Clear();

            SelectToolPatch.UpdateColor();
        }

        void Join()
        {
            GameClient.ConnectToHost();
        }
	}
}
