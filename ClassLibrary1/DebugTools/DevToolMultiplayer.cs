using System;
using System.Diagnostics;
using System.IO;
using ImGuiNET;
using ONI_MP.Cloud;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay.Platforms.EOS;
using ONI_MP.Networking.Relay.Platforms.Steam;
using ONI_MP.Networking.Platforms.Steam;
using ONI_MP.Networking.Components;
using UnityEngine;
using static STRINGS.UI;

namespace ONI_MP.DebugTools
{
    public class DevToolMultiplayer : DevTool
    {
        private Vector2 scrollPos = Vector2.zero;
        DebugConsole console = null;

        // Relay selection
        private static readonly string[] Platforms = { "Steam", "Epic Online Services" };
        private static int _currentPlatformIndex = 0;

        // Player color
        private bool useRandomColor = false;
        private Vector3 playerColor = new Vector3(1f, 1f, 1f);

        // Alert popup
        private bool showRestartPrompt = false;

        private static readonly string ModDirectory = Path.Combine(
            Path.GetDirectoryName(typeof(DevToolMultiplayer).Assembly.Location),
            "oni_mp.dll"
        );

        public DevToolMultiplayer()
        {
            Name = "Multiplayer";
            RequiresGameRunning = false;
            console = DebugConsole.Init();

            _currentPlatformIndex = Configuration.GetClientProperty<int>("Platform");

            ColorRGB loadedColor = Configuration.GetClientProperty<ColorRGB>("PlayerColor");
            playerColor = new Vector3(loadedColor.R / 255, loadedColor.G / 255, loadedColor.B / 255);
            useRandomColor = Configuration.GetClientProperty<bool>("UseRandomPlayerColor");

            OnInit += () => Init();
            OnUpdate += () => Update();
            OnUninit += () => UnInit();
        }

        void Init()
        {

        }

        void Update()
        {

        }

        void UnInit()
        {

        }

        protected override void RenderTo(DevPanel panel)
        {
            // Begin scroll region
            ImGui.BeginChild("ScrollRegion", new Vector2(0, 0), true, ImGuiWindowFlags.HorizontalScrollbar);

            if (ImGui.Button("Open Mod Directory"))
            {
                string dir = Path.GetDirectoryName(ModDirectory);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
            ImGui.SameLine();
            if (ImGui.Button("Toggle Debug Console"))
            {
                console?.Toggle();
                Debug.Log("Toggled Debug Console");
            }
            console?.ShowWindow();

            ImGui.NewLine();
            ImGui.Separator();

            string previewValue = Platforms[_currentPlatformIndex];
            if (ImGui.BeginCombo("Platform Selector", previewValue))
            {
                for (int i = 0; i < Platforms.Length; i++)
                {
                    bool isSelected = (_currentPlatformIndex == i);
                    if (ImGui.Selectable(Platforms[i], isSelected))
                    {
                        if (_currentPlatformIndex != i)
                        {
                            _currentPlatformIndex = i;
                            Configuration.SetClientProperty("Platform", _currentPlatformIndex);
                            MultiplayerMod.singleton.ReInitializeNetworkPlatform(); // Maybe prompt a restart instead
                            showRestartPrompt = true;
                        }
                    }


                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                // End the Combo Box context
                ImGui.EndCombo();
            }

            if(showRestartPrompt)
            {
                ShowAlertPrompt(ref showRestartPrompt, "Network Relay Changed!", "The network relay has changed!\nA restart is required!", "Restart", "Ignore", () =>
                {
                    App.instance.Restart();
                });
            }

            if (ImGui.CollapsingHeader("Player Color"))
            {
                if (ImGui.Checkbox("Use Random Color", ref useRandomColor))
                {
                    Configuration.SetClientProperty<bool>("UseRandomPlayerColor", useRandomColor);
                }

                if (ImGui.ColorPicker3("Player Color", ref playerColor))
                {
                    ColorRGB colorRGB = new ColorRGB();
                    colorRGB.R = (byte)(255 * playerColor.x);
                    colorRGB.G = (byte)(255 * playerColor.y);
                    colorRGB.B = (byte)(255 * playerColor.z);
                    Configuration.SetClientProperty<ColorRGB>("PlayerColor", colorRGB);
                }
            }

            // Multiplayer status section
            if (MultiplayerMod.WasPlatformInitialized)
            {
                ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), "Multiplayer Active");

                if (ImGui.Button("Create Lobby"))
                {
                    PacketSender.Platform.Lobby.CreateLobby(onSuccess: () =>
                    {
                        SpeedControlScreen.Instance?.Unpause(false);
                    });
                }

                ImGui.SameLine();
                if (ImGui.Button("Leave Lobby"))
                {
                    PacketSender.Platform.Lobby.LeaveLobby();
                }

                ImGui.NewLine();
                if (ImGui.Button("Client Disconnect"))
                {
                    PacketSender.Platform.GameClient.CacheCurrentServer();
                    PacketSender.Platform.GameClient.Disconnect();
                }

                ImGui.SameLine();
                if (ImGui.Button("Reconnect"))
                {
                    PacketSender.Platform.GameClient.ReconnectFromCache();
                }

                ImGui.NewLine();
                ImGui.Separator();

                ImGui.Text("Session details:");
                ImGui.Text($"Platform: {PacketSender.Platform.ID}");
                ImGui.Text($"Connected clients: {PacketSender.Platform.ConnectedClients.Count}");
                ImGui.Text($"Is Host: {MultiplayerSession.IsHost}");
                ImGui.Text($"Is Client: {MultiplayerSession.IsClient}");
                ImGui.Text($"In Session: {MultiplayerSession.InSession}");
                ImGui.Text($"Local ID: {MultiplayerSession.LocalId}");
                ImGui.Text($"Host ID: {MultiplayerSession.HostId}");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.3f, 1f), "Network relay not initialized!");
                if (PacketSender.Platform.ID == "Epic Online Services")
                {
                    if (!EOSManager.Instance.LoggedIn)
                    {
                        if (ImGui.Button("Authenticate Account Portal"))
                        {
                            EOSManager.Instance.LoginWithAccountPortal();
                        }

                        if (ImGui.Button("Authenticate Persistent"))
                        {
                            EOSManager.Instance.AttemptLoginWithPersistentAuth();
                        }
                    }
                    else
                    {
                        if (ImGui.Button("Logout"))
                            EOSManager.Instance.Logout();

                        if (ImGui.Button("Clear persistent auth"))
                            EOSManager.Instance.ClearPersistentAuth();

                        if (ImGui.Button("Login via Connect Interface"))
                            EOSManager.Instance.HandleConnect();
                    }
                }
            }

            ImGui.Separator();

            try
            {
                if (MultiplayerSession.InSession)
                {
                    if (!MultiplayerSession.IsHost)
                    {
                        int? ping = PacketSender.Platform.GameClient.GetPingToHost();
                        string pingDisplay = ping >= 0 ? $"{ping} ms" : "Pending...";
                        ImGui.Text($"Ping to Host: {pingDisplay}");
                    }
                    else
                    {
                        ImGui.Text("Hosting multiplayer session.");
                        if (ImGui.Button("Test Hard Sync"))
                        {
                            GameServerHardSync.PerformHardSync();
                        }
                    }

                    ImGui.Separator();
                    ImGui.Text("Google Drive");

                    if (MultiplayerSession.IsHost && ImGui.Button("Test Upload"))
                    {
                        GoogleDriveUtils.UploadSaveFile();
                    }

                    DrawPlayerList();
                }
                else
                {
                    ImGui.Text("Not in a multiplayer session.");
                }
            }
            catch (Exception e)
            {
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), $"Error: {e.Message}");
            }

            ImGui.EndChild();
        }

        private void DrawPlayerList()
        {
            var players = PacketSender.Platform.Lobby.GetAllLobbyMembers();

            ImGui.Separator();
            ImGui.Text("Players in Lobby:");
            if (players.Count == 0)
            {
                ImGui.Text("<none>");
            }
            else
            {
                foreach (string playerId in players)
                {
                    var playerName = PacketSender.Platform.GetPlayerName(playerId);
                    bool isHost = MultiplayerSession.HostId == playerId;

                    if (isHost)
                        ImGui.TextColored(new Vector4(0.8f, 0.9f, 1f, 1f), $"[HOST] {playerName} ({playerId})");
                    else
                        ImGui.Text($"{playerName} ({playerId})");
                }
            }
        }

        private void ShowAlertPrompt(
            ref bool isVisible,
            string title,
            string message,
            string confirmText,
            string cancelText,
            System.Action onConfirmAction)
        {
            if (ImGui.BeginPopupModal(title, ref isVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(message);
                ImGui.Separator();

                if (ImGui.Button(confirmText, new Vector2(150, 0)))
                {
                    isVisible = false;

                    onConfirmAction.Invoke();
                }

                ImGui.SameLine();
                if (ImGui.Button(cancelText, new Vector2(150, 0)))
                {
                    isVisible = false;
                }
                ImGui.EndPopup();
            }
        }
    }
}
