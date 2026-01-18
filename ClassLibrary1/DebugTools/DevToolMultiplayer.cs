// Keep this to only windows, Mac is not built with the Devtool framework so it doesn't have access to the DevTool class and just crashes
#if DEBUG //OS_WINDOWS || DEBUG

using System;
using System.Diagnostics;
using System.IO;
using ImGuiNET;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Components;
using UnityEngine;
using static STRINGS.UI;
using Steamworks;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking.Profiling;
using System.Text;
using ONI_MP.Patches.ToolPatches;
using ONI_MP.Tests;

namespace ONI_MP.DebugTools
{
    public class DevToolMultiplayer : DevTool
    {
        private Vector2 scrollPos = Vector2.zero;
        DebugConsole console = null;
        PacketTracker packetTracker = null;

        // Player color
        private bool useRandomColor = false;
        private Vector3 playerColor = new Vector3(1f, 1f, 1f);

        // Alert popup
        private bool showRestartPrompt = false;

        // Open player profile
        private ulong? selectedPlayer = null;

        // Network relay
        private int selectedRelayType = 0; // 0 = Steam, 1 = LAN
        private int selectedLanType = 0; // 0 = Riptide, 1 = LiteNetLib
        private string hostIP = "";
        private int hostPort = 7777;
        private string clientIP = "";
        private int clientPort = 7777;
        LanSettings settings_host = new LanSettings();
        LanSettings settings_client = new LanSettings();

        private static readonly string ModDirectory = Path.Combine(
            Path.GetDirectoryName(typeof(DevToolMultiplayer).Assembly.Location),
            "oni_mp.dll"
        );

        public DevToolMultiplayer()
        {
            Name = "Multiplayer";
            RequiresGameRunning = false;
            console = DebugConsole.Init();
            packetTracker = PacketTracker.Init();

            ColorRGB loadedColor = Configuration.GetClientProperty<ColorRGB>("PlayerColor");
            playerColor = new Vector3(loadedColor.R / 255, loadedColor.G / 255, loadedColor.B / 255);
            useRandomColor = Configuration.GetClientProperty<bool>("UseRandomPlayerColor");

            OnInit += () => Init();
            OnUpdate += () => Update();
            OnUninit += () => UnInit();

            selectedRelayType = Configuration.Instance.Host.NetworkRelay;
            hostIP = Configuration.Instance.Host.LanSettings.Ip;
            hostPort = Configuration.Instance.Host.LanSettings.Port;
            settings_host.Ip = hostIP;
            settings_host.Port = hostPort;

            clientIP = Configuration.Instance.Client.LanSettings.Ip;
            clientPort = Configuration.Instance.Client.LanSettings.Port;
            settings_client.Ip = clientIP;
            settings_client.Port = clientPort;
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

        public override void RenderTo(DevPanel panel)
        {
            ImGui.BeginChild("ScrollRegion", new Vector2(0, 0), true);

            if (ImGui.BeginTabBar("MultiplayerTabs"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    DrawGeneralTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Session"))
                {
                    DrawSessionTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Network"))
                {
                    DrawNetworkTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Debug"))
                {
                    DrawDebugTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Unit Tests"))
                {
                    DrawTestsTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Console"))
                {
                    DrawConsoleTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.EndChild();

            console?.ShowWindow();
            packetTracker?.ShowWindow();
            GameClientProfiler.DrawImGuiPopout();
            GameServerProfiler.DrawImGuiPopout();
        }

        private void DrawGeneralTab()
        {
            if (ImGui.Button("Open Mod Directory"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.GetDirectoryName(ModDirectory),
                    UseShellExecute = true
                });
            }

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Player Color", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Checkbox("Use Random Color", ref useRandomColor))
                    Configuration.SetClientProperty("UseRandomPlayerColor", useRandomColor);

                if (ImGui.ColorPicker3("Color", ref playerColor))
                {
                    Configuration.SetClientProperty("PlayerColor", new ColorRGB
                    {
                        R = (byte)(playerColor.x * 255),
                        G = (byte)(playerColor.y * 255),
                        B = (byte)(playerColor.z * 255),
                    });
                }
            }
        }

        private void DrawSessionTab()
        {
            if(MultiplayerSession.InSession)
                ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), "Multiplayer Active");
            else
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "Multiplayer Not Active");
            ImGui.Separator();

            switch (NetworkConfig.relay) 
            {
                case NetworkConfig.NetworkRelay.STEAM:
                    if (ImGui.Button("Create Lobby"))
                    {
                        SteamLobby.CreateLobby(onSuccess: () =>
                        {
                            SpeedControlScreen.Instance?.Unpause(false);
                            Game.Instance.Trigger(MP_HASHES.OnMultiplayerGameSessionInitialized);
                        });
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Leave Lobby"))
                        SteamLobby.LeaveLobby();
                    break;
                case NetworkConfig.NetworkRelay.RIPTIDE:
                    if (ImGui.Button("Start Lan"))
                    {
                        MultiplayerSession.Clear();
                        Networking.GameServer.Start();
                        SelectToolPatch.UpdateColor();
                        Game.Instance.Trigger(MP_HASHES.OnMultiplayerGameSessionInitialized);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Stop Lan"))
                    {
                        if (MultiplayerSession.IsHost)
                            Networking.GameServer.Shutdown();

                        if (MultiplayerSession.IsClient)
                            GameClient.Disconnect();

                        NetworkIdentityRegistry.Clear();
                        MultiplayerSession.Clear();

                        SelectToolPatch.UpdateColor();
                    }
                    break;
                default:
                    break;
            }

            ImGui.SameLine();
            if (ImGui.Button("Client Disconnect"))
            {
                GameClient.CacheCurrentServer();
                GameClient.Disconnect();
            }

            ImGui.SameLine();
            if (ImGui.Button("Reconnect"))
                GameClient.ReconnectFromCache();

            ImGui.Separator();
            DisplaySessionDetails();

            if (NetworkConfig.relay.Equals(NetworkConfig.NetworkRelay.STEAM))
            {
                if (MultiplayerSession.InSession)
                    DrawPlayerList();
                else
                    ImGui.TextDisabled("Not in a multiplayer session.");
            } else
            {
                ImGui.TextDisabled("No access to a player list.");
            }
        }

        private void DrawNetworkTab()
        {
            DrawNetworkRelayDetails();
            if (!MultiplayerSession.InSession)
            {
                ImGui.TextDisabled("Not connected.");
                return;
            }

            DisplayNetworkStatistics();

            if (ImGui.CollapsingHeader("Packet Tracker"))
            {
                ImGui.Indent();
                if (ImGui.Button("Toggle Popout"))
                    packetTracker?.Toggle();
                packetTracker?.ShowInTab();
                ImGui.Unindent();
            }

            if (MultiplayerSession.IsHost)
            {
                ImGui.Separator();
                if (ImGui.Button("Test Hard Sync"))
                    GameServerHardSync.PerformHardSync();
            }
        }

        private void DrawDebugTab()
        {
            DisplayProfilers();
            ImGui.Separator();
            DisplayNetIdHolders();
        }

        private void DrawTestsTab()
        {
            if (ImGui.Button("Riptide Smoke Test"))
            {
                RiptideSmokeTest.Run();
            }
            ImGui.SameLine();
            if (ImGui.Button("LiteNetLib Smoke Test"))
            {
                LiteNetLibSmokeTest.Run(7777);
            }

            if (ImGui.Button("Explode"))
            {
                MultiplayerSession.InSession = true;
            }

            if (ImGui.Button("Start Current Config Server"))
            {
                NetworkConfig.RelayServer.Start();
            }

            ImGui.SameLine();
            if (ImGui.Button("Stop Current Config Server"))
            {
                NetworkConfig.RelayServer.Stop();
            }
        }

        private void DrawConsoleTab()
        {
            if (ImGui.Button("Popout"))
                console?.Toggle();
            ImGui.SameLine();
            console?.ShowInTab();
        }

        public void DisplaySessionDetails()
        {
            ImGui.Text("Session details:");
            ImGui.Text($"Connected clients: {(MultiplayerSession.InSession ? (MultiplayerSession.PlayerCursors.Count + 1) : 0)}");
            ImGui.Text($"Is Host: {MultiplayerSession.IsHost}");
            ImGui.Text($"Is Client: {MultiplayerSession.IsClient}");
            ImGui.Text($"In Session: {MultiplayerSession.InSession}");
            ImGui.Text($"Local ID: {MultiplayerSession.LocalUserID}");
            ImGui.Text($"Host ID: {MultiplayerSession.HostUserID}");
        }

        private void DrawPlayerList()
        {
            var players = SteamLobby.GetAllLobbyMembers();

            ImGui.Separator();
            ImGui.Text("Players in Lobby:");

            string self = $"[YOU] {SteamFriends.GetPersonaName()} ({MultiplayerSession.LocalUserID})";

            if (players.Count == 0)
            {
                ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), self);
                return;
            }

            if (MultiplayerSession.HostUserID == MultiplayerSession.LocalUserID)
                self = $"[YOU|HOST] {SteamFriends.GetPersonaName()} ({MultiplayerSession.LocalUserID})";

            ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), self);

            foreach (var playerId in players)
            {
                string playerName = SteamFriends.GetFriendPersonaName(playerId);
                bool isHost = MultiplayerSession.HostUserID == playerId.m_SteamID;

                string label = isHost
                    ? $"[HOST] {playerName} ({playerId})"
                    : $"{playerName} ({playerId})";

                bool isSelected = selectedPlayer.HasValue && selectedPlayer.Value == playerId.m_SteamID;

                if (ImGui.Selectable(label, isSelected))
                {
                    selectedPlayer = playerId.m_SteamID;
                }

                // Right-click context menu
                if (ImGui.BeginPopupContextItem(playerId.ToString()))
                {
                    if (ImGui.MenuItem("Open Steam Profile"))
                    {
                        SteamFriends.ActivateGameOverlayToUser("steamid", playerId);
                    }

                    ImGui.EndPopup();
                }
            }
        }

        public void DisplayNetworkStatistics()
        {
            if(!MultiplayerSession.InSession)
                return;

            ImGui.Separator();
            ImGui.Text("Network Statistics");
            // TODO Update:
            //ImGui.Text($"Ping: {GameClient.GetPingToHost()}");
            //ImGui.Text($"Quality(L/R): {GameClient.GetLocalPacketQuality():0.00} / {GameClient.GetRemotePacketQuality():0.00}");
            //ImGui.Text($"Unacked Reliable: {GameClient.GetUnackedReliable()}");
            //ImGui.Text($"Pending Unreliable: {GameClient.GetPendingUnreliable()}");
            //ImGui.Text($"Queue Time: {GameClient.GetUsecQueueTime() / 1000}ms");
            ImGui.Spacing();
            ImGui.Text($"Latency: {Utils.NetworkStateToString(NetworkIndicatorsScreen.latencyState)}");
            ImGui.Text($"Jitter: {Utils.NetworkStateToString(NetworkIndicatorsScreen.jitterState)}");
            ImGui.Text($"Packet Loss: {Utils.NetworkStateToString(NetworkIndicatorsScreen.packetlossState)}");
            ImGui.Text($"Server Performance: {Utils.NetworkStateToString(NetworkIndicatorsScreen.serverPerformanceState)}");

            // Sync Statistics (Host only)
            if (MultiplayerSession.IsHost)
            {
                ImGui.Separator();
                if (ImGui.CollapsingHeader("Sync Statistics"))
                {
                    float fps = 1f / Time.unscaledDeltaTime;
                    ImGui.Text($"FPS: {fps:F0} | Clients: {MultiplayerSession.ConnectedPlayers.Count}");
                    ImGui.Spacing();

                    foreach (var m in SyncStats.AllMetrics)
                    {
                        if (m.LastSyncTime > 0)
                        {
                            ImGui.Text($"{m.Name}: {m.TimeRemaining:F1}s | {m.LastItemCount} items, {m.LastPacketBytes}B, {m.LastDurationMs:F1}ms");
                        }
                        else
                        {
                            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), $"{m.Name}: waiting...");
                        }
                    }
                }
            }
        }
		
        private string netIdFilter = string.Empty;
		public void DisplayNetIdHolders()
		{
			if (ImGui.CollapsingHeader("Net Id Holders"))
			{
				var all_identities = NetworkIdentityRegistry.AllIdentities;

				ImGui.InputText("Filter", ref netIdFilter, 64);
				ImGui.Separator();

				if (ImGui.BeginTable("net_identity_table", 2,
						ImGuiTableFlags.Borders |
						ImGuiTableFlags.RowBg |
						ImGuiTableFlags.ScrollY, new UnityEngine.Vector2(0, 400)))
				{
					ImGui.TableSetupColumn("Name");
					ImGui.TableSetupColumn("Network ID");

					ImGui.TableHeadersRow();

					foreach (var identity in all_identities)
					{
						string identityName = identity.gameObject.name;
						string identityNetId = identity.NetId.ToString();

						if (!string.IsNullOrEmpty(netIdFilter))
						{
							bool matchesType =
								identityName.IndexOf(netIdFilter, StringComparison.OrdinalIgnoreCase) >= 0;

							bool matchesId =
								identityNetId.IndexOf(netIdFilter, StringComparison.OrdinalIgnoreCase) >= 0;

							if (!matchesType && !matchesId)
								continue;
						}

						ImGui.TableNextRow();

						ImGui.TableSetColumnIndex(0);
						ImGui.Text(identityName);

						ImGui.TableSetColumnIndex(1);
						ImGui.Text(identityNetId);
					}

					ImGui.EndTable();
				}
			}
		}
	
        public void DisplayProfilers()
        {
            if (ImGui.BeginTable("profilers", 2))
            {
                ImGui.TableNextColumn();
                ImGui.Text("Server");
                GameServerProfiler.DrawImGuiInTab();

                // Why can I never interact with the toggles or buttons of the second one even if I saw them around

                ImGui.TableNextColumn();
                ImGui.Text("Client");
                GameClientProfiler.DrawImGuiInTab();

                ImGui.EndTable();
            }
        }

        public void DrawNetworkRelayDetails()
        {
            ImGui.Text("Network Relay Settings");

            string[] display_options = new string[] { "Steam", "LAN/Riptide", "Lan/LiteNetLib" };
            ImGui.Text($"Currently used relay: {display_options[(int)NetworkConfig.relay]}");

            string[] options = new string[] { "Steam", "LAN" };
            // Dropdown for Steam/LAN
            ImGui.Combo("Relay Type", ref selectedRelayType, options, options.Length);

            // Only show LAN-specific fields if LAN is selected
            if (selectedRelayType == 1)
            {
                ImGui.Indent();
                ImGui.Separator();

                string[] lan_options = new string[] { "Riptide", "LiteNetLib" };
                ImGui.Combo("Lan Type", ref selectedLanType, lan_options, lan_options.Length);
                ImGui.Separator();

                // Host section
                ImGui.Text("Host Settings (Used for hosting a server)");
                ImGui.InputText("Host IP", ref hostIP, 64);
                ImGui.InputInt("Host Port", ref hostPort);
                settings_host.Ip = hostIP;
                settings_host.Port = hostPort;

                ImGui.Separator();

                // Client section
                ImGui.Text("Client Settings (The server you are connecting too)");
                ImGui.InputText("Client IP", ref clientIP, 64);
                ImGui.InputInt("Client Port", ref clientPort);
                settings_client.Ip = hostIP;
                settings_client.Port = hostPort;
                ImGui.Unindent();
            }

            if (ImGui.Button("Save & Apply"))
            {
                Configuration.Instance.Host.LanSettings.Ip = hostIP;
                Configuration.Instance.Host.LanSettings.Port = hostPort;
                Configuration.Instance.Client.LanSettings.Ip = clientIP;
                Configuration.Instance.Client.LanSettings.Port = clientPort;

                NetworkConfig.NetworkRelay selected_relay = NetworkConfig.NetworkRelay.STEAM;
                if (selectedRelayType == 0)
                {
                    selected_relay = NetworkConfig.NetworkRelay.STEAM;
                }
                else
                {
                    if(selectedLanType == 0)
                    {
                        selected_relay = NetworkConfig.NetworkRelay.RIPTIDE;
                    } else
                    {
                        selected_relay = NetworkConfig.NetworkRelay.LITENETLIB;
                    }
                }
                Configuration.Instance.Host.NetworkRelay = (int)selected_relay;
                NetworkConfig.UpdateRelay(selected_relay);
                Configuration.Instance.Save();
            }
        }
    }
}
#endif