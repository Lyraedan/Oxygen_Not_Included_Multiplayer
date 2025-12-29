using ONI_MP.Networking;
using ONI_MP.Networking.States;
using Steamworks;
using System;
using UnityEngine;

namespace ONI_MP.DebugTools
{
    public class DebugMenu : MonoBehaviour
    {
        private static DebugMenu _instance;

        private bool showMenu = false;
        private Rect windowRect = new Rect(10, 10, 300, 450); // 稍微加大一点
        private HierarchyViewer hierarchyViewer;
        private DebugConsole debugConsole;

        private Vector2 scrollPosition = Vector2.zero;

        // 直连相关
        private bool showDirectConnect = false;
        private string directConnectIP = "";
        private string directConnectPort = "11000";
        private string directConnectStatus = "";

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
            // Shift + F1 打开调试菜单
            if (Input.GetKeyDown(KeyCode.F1) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                showMenu = !showMenu;
            }

            // Shift + F2 快速打开直连窗口
            if (Input.GetKeyDown(KeyCode.F2) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                showDirectConnect = !showDirectConnect;
            }
        }

        private void OnGUI()
        {
            // 绘制主调试菜单
            if (showMenu)
            {
                GUIStyle windowStyle = new GUIStyle(GUI.skin.window) { padding = new RectOffset(10, 10, 20, 20) };
                windowRect = GUI.ModalWindow(888, windowRect, DrawMenuContents, "DEBUG MENU", windowStyle);
            }

            // 绘制直连窗口
            if (showDirectConnect)
            {
                DrawDirectConnectWindow();
            }
        }

        private void DrawMenuContents(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.Width(windowRect.width - 20), GUILayout.Height(windowRect.height - 40));

            GUILayout.Label("<b>== 工具 ==</b>");
            
            if (GUILayout.Button("Toggle Hierarchy Viewer"))
                hierarchyViewer.Toggle();

            GUILayout.Space(10);
            GUILayout.Label("<b>== 准备状态 ==</b>");

            if (GUILayout.Button("Send Unready Packet"))
                ReadyManager.SendReadyStatusPacket(ClientReadyState.Unready);

            if (GUILayout.Button("Send Ready Packet"))
                ReadyManager.SendReadyStatusPacket(ClientReadyState.Ready);

            GUILayout.Space(10);
            GUILayout.Label("<b>== 网络 ==</b>");

            // 显示当前连接模式
            string modeText = DirectConnection.Mode == ConnectionMode.DirectIP ? "<color=green>直连模式</color>" : "<color=cyan>Steam P2P</color>";
            GUILayout.Label($"当前模式: {modeText}");

            if (GUILayout.Button("打开直连窗口 (Shift+F2)"))
            {
                showDirectConnect = !showDirectConnect;
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>== 玩家列表 ==</b>");
            DrawPlayerList();

            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        #region 直连窗口

        private Rect directConnectRect = new Rect(0, 0, 380, 350);
        private bool directConnectRectInitialized = false;

        private void DrawDirectConnectWindow()
        {
            // 居中窗口
            if (!directConnectRectInitialized)
            {
                directConnectRect.x = (Screen.width - directConnectRect.width) / 2;
                directConnectRect.y = (Screen.height - directConnectRect.height) / 2;
                directConnectRectInitialized = true;
            }

            GUIStyle windowStyle = new GUIStyle(GUI.skin.window) { padding = new RectOffset(15, 15, 25, 15) };
            directConnectRect = GUI.Window(889, directConnectRect, DrawDirectConnectContents, "直连模式 / Direct Connect", windowStyle);
        }

        private void DrawDirectConnectContents(int windowID)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 };
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fixedHeight = 32 };
            GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 13, fixedHeight = 24 };
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };

            // 状态信息
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label($"<b>连接状态:</b> {DirectConnection.GetConnectionInfo()}", labelStyle);
            GUILayout.Label($"<b>本机 IP:</b> <color=yellow>{DirectConnection.GetLocalIPAddress()}</color>", labelStyle);
            GUILayout.Label($"<b>模式:</b> {(DirectConnection.Mode == ConnectionMode.DirectIP ? "<color=green>直连</color>" : "<color=cyan>Steam P2P</color>")}", labelStyle);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // 输入区域
            GUILayout.Label("主机 IP 地址:", labelStyle);
            directConnectIP = GUILayout.TextField(directConnectIP, textFieldStyle);

            GUILayout.Space(5);

            GUILayout.Label("端口 (默认 11000):", labelStyle);
            directConnectPort = GUILayout.TextField(directConnectPort, textFieldStyle);

            GUILayout.Space(15);

            // 按钮区域
            if (!DirectConnection.IsServerRunning && !DirectConnection.IsClientConnected)
            {
                // 创建房间
                if (GUILayout.Button("🎮 创建房间 (作为主机)", buttonStyle))
                {
                    int port = int.TryParse(directConnectPort, out int p) ? p : DirectConnection.DEFAULT_PORT;
                    if (DirectConnection.StartServer(port))
                    {
                        directConnectStatus = $"<color=green>✓ 房间已创建！\n告诉朋友连接: {DirectConnection.GetLocalIPAddress()}:{port}</color>";
                    }
                    else
                    {
                        directConnectStatus = "<color=red>✗ 创建失败，端口可能被占用</color>";
                    }
                }

                GUILayout.Space(5);

                // 加入房间
                if (GUILayout.Button("🔗 加入房间 (作为客户端)", buttonStyle))
                {
                    if (string.IsNullOrWhiteSpace(directConnectIP))
                    {
                        directConnectStatus = "<color=red>✗ 请输入主机 IP 地址</color>";
                    }
                    else
                    {
                        int port = int.TryParse(directConnectPort, out int p) ? p : DirectConnection.DEFAULT_PORT;
                        directConnectStatus = $"<color=yellow>正在连接 {directConnectIP}:{port}...</color>";
                        
                        if (DirectConnection.Connect(directConnectIP, port))
                        {
                            directConnectStatus = $"<color=green>✓ 已连接到 {directConnectIP}:{port}</color>";
                        }
                        else
                        {
                            directConnectStatus = "<color=red>✗ 连接失败，请检查 IP 和端口</color>";
                        }
                    }
                }
            }
            else
            {
                // 已连接状态
                GUILayout.BeginVertical(boxStyle);
                if (DirectConnection.IsServerRunning)
                {
                    GUILayout.Label($"<color=green>● 服务器运行中</color>", labelStyle);
                    GUILayout.Label($"已连接客户端: {DirectConnection.GetConnectedClientCount()}", labelStyle);
                }
                else
                {
                    GUILayout.Label($"<color=green>● 已连接到主机</color>", labelStyle);
                }
                GUILayout.EndVertical();

                GUILayout.Space(10);

                // 断开按钮
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("❌ 断开连接 / 关闭房间", buttonStyle))
                {
                    if (DirectConnection.IsServerRunning)
                    {
                        DirectConnection.StopServer();
                        directConnectStatus = "<color=yellow>房间已关闭</color>";
                    }
                    else
                    {
                        DirectConnection.Disconnect();
                        directConnectStatus = "<color=yellow>已断开连接</color>";
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(10);

            // 状态消息
            if (!string.IsNullOrEmpty(directConnectStatus))
            {
                GUILayout.Label(directConnectStatus, labelStyle);
            }

            GUILayout.FlexibleSpace();

            // 关闭按钮
            if (GUILayout.Button("关闭窗口", buttonStyle))
            {
                showDirectConnect = false;
            }

            GUI.DragWindow();
        }

        #endregion

        private void DrawPlayerList()
        {
            GUILayout.Label("Players in Lobby:", GUI.skin.label);

            var players = SteamLobby.GetAllLobbyMembers();
            if (players.Count == 0)
            {
                GUILayout.Label("<none>", GUI.skin.label);
            }
            else
            {
                foreach (CSteamID playerId in players)
                {
                    var playerName = SteamFriends.GetFriendPersonaName(playerId);
                    string prefix = (MultiplayerSession.HostSteamID == playerId) ? "[HOST] " : "";
                    GUILayout.Label($"{prefix}{playerName} ({playerId})", GUI.skin.label);
                }
            }

            // 直连模式下显示客户端数量
            if (DirectConnection.Mode == ConnectionMode.DirectIP && DirectConnection.IsServerRunning)
            {
                GUILayout.Space(5);
                GUILayout.Label($"<color=cyan>[直连] 客户端数量: {DirectConnection.GetConnectedClientCount()}</color>", 
                    new GUIStyle(GUI.skin.label) { richText = true });
            }
        }
    }
}
