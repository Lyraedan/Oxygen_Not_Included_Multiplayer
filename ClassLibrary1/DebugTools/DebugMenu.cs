using ONI_MP.Networking;
using Utils = ONI_MP.Misc.Utils;
using UnityEngine;
using ONI_MP.Networking.Components;
using KMod;
using ONI_MP.Networking.Packets.World;
using Steamworks;
using System;
using ONI_MP.Cloud;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay.Platforms.Steam;
using ONI_MP.Networking.Platforms.Steam;

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

            GUILayout.Space(10);
            GUILayout.Label("Multiplayer");

            if (GUILayout.Button("Create Lobby"))
                PacketSender.Platform.Lobby.CreateLobby(onSuccess: () => {
                    SpeedControlScreen.Instance?.Unpause(false);
                });

            if (GUILayout.Button("Leave lobby"))
                PacketSender.Platform.Lobby.LeaveLobby();

            if (GUILayout.Button("Client disconnect"))
            {
                PacketSender.Platform.GameClient.CacheCurrentServer();
                PacketSender.Platform.GameClient.Disconnect();
            }

            if (GUILayout.Button("Reconnect"))
                PacketSender.Platform.GameClient.ReconnectFromCache();

            GUILayout.Space(10);
            GUILayout.Label("Session details");
            GUILayout.Label($"Platform: {PacketSender.Platform.ID}");
            GUILayout.Label($"Connected clients: {PacketSender.Platform.ConnectedClients.Count}");
            GUILayout.Label($"Is Host: {MultiplayerSession.IsHost}");
            GUILayout.Label($"Is Client: {MultiplayerSession.IsClient}");
            GUILayout.Label($"In Session: {MultiplayerSession.InSession}");
            GUILayout.Label($"Local ID: {MultiplayerSession.LocalId}");
            GUILayout.Label($"Host ID: {MultiplayerSession.HostId}");

            GUILayout.Space(10);

            try
            {
                if (MultiplayerSession.InSession)
                {
                    if (!MultiplayerSession.IsHost)
                    {
                        int? ping = PacketSender.Platform.GameClient.GetPingToHost();
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

            var players = PacketSender.Platform.Lobby.GetAllLobbyMembers();
            if (players.Count == 0)
            {
                GUILayout.Label("<none>", UnityEngine.GUI.skin.label);
            }
            else
            {
                foreach (string playerId in players)
                {
                    var playerName = PacketSender.Platform.GetPlayerName(playerId);
                    string prefix = (MultiplayerSession.HostId == playerId) ? "[HOST] " : "";
                    GUILayout.Label($"{prefix}{playerName} ({playerId})", UnityEngine.GUI.skin.label);
                }
            }
        }


    }
}
