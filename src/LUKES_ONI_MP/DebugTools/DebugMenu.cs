using ONI_MP.Networking;
using Utils = ONI_MP.Misc.Utils;
using UnityEngine;
using ONI_MP.Networking.Components;
using KMod;
using ONI_MP.Networking.Packets.World;
using Steamworks;
using System;
using ONI_MP.Cloud;

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
            if (_instance != null) return;

            GameObject go = new GameObject("ONI_MP_DebugMenu");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DebugMenu>();
        }

        private void Awake()
        {
            hierarchyViewer = gameObject.AddComponent<HierarchyViewer>();
            debugConsole = gameObject.AddComponent<DebugConsole>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
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
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.Width(windowRect.width - 20), GUILayout.Height(windowRect.height - 40));

            if (GUILayout.Button("Toggle Hierarchy Viewer"))
                hierarchyViewer.Toggle();

            if (GUILayout.Button("Toggle Debug Console"))
                debugConsole.Toggle();

            if (GUILayout.Button("Create Lobby"))
                SteamLobby.CreateLobby(onSuccess: () => {
                    SpeedControlScreen.Instance?.Unpause(false);
                });

            if (GUILayout.Button("Leave lobby"))
                SteamLobby.LeaveLobby();

            if (GUILayout.Button("Client disconnect"))
            {
                GameClient.CacheCurrentServer();
                GameClient.Disconnect();
            }

            if (GUILayout.Button("Reconnect"))
                GameClient.ReconnectFromCache();

            GUILayout.Space(10);

            try
            {
                if (MultiplayerSession.InSession)
                {
                    if (!MultiplayerSession.IsHost)
                    {
                        int? ping = GameClient.GetPingToHost();
                        string pingDisplay = ping >= 0 ? $"{ping} ms" : "Pending...";
                        GUILayout.Label($"Ping to Host: {pingDisplay}");
                    }
                    else
                    {
                        GUILayout.Label("Hosting multiplayer session.");
                        if (GUILayout.Button("Test Hard sync"))
                            GameServerHardSync.PerformHardSync();
                    }

                    GUILayout.Space(10);
                    GUILayout.Label($"Google Drive");
                    if (MultiplayerSession.IsHost)
                    {
                        if (GUILayout.Button("Test Upload"))
                        {
                            GoogleDriveUtils.UploadSaveFile();
                        }
                    }
                    //DrawPlayerList();
                }
                else
                {
                    GUILayout.Label("Not in a multiplayer session.");
                }
            } catch(Exception e)
            {

            }

            GUILayout.Space(20);
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        private void DrawPlayerList()
        {
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
