using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.States;
using ONI_MP.Networking.Transport.Lan;
using Steamworks;
using System;
using UnityEngine;
using SteamworksClient = ONI_MP.Networking.Transport.Steam.SteamworksClient;

namespace ONI_MP.DebugTools
{
	public class NetworkStatisticsMenu : MonoBehaviour
	{
		private static NetworkStatisticsMenu _instance;

		private bool showMenu = false;
		private Rect windowRect = new Rect(10, 10, 250, 300); // Position and size

		private Vector2 scrollPosition = Vector2.zero;


		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void Init()
		{
			if (_instance != null) return;

			GameObject go = new GameObject("ONI_MP_NetworkStatisticsMenu");
			DontDestroyOnLoad(go);
			_instance = go.AddComponent<NetworkStatisticsMenu>();
		}

		private void Awake()
		{

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
			windowRect = GUI.ModalWindow(888, windowRect, DrawMenuContents, "Network Statistics", windowStyle);
		}

		private void DrawMenuContents(int windowID)
		{
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.Width(windowRect.width - 20), GUILayout.Height(windowRect.height - 40));

			int ping = -1;
			float qualityL = -1;
            float qualityR = -1;

			switch (NetworkConfig.transport)
			{
				case NetworkConfig.NetworkTransport.STEAMWORKS:
					ping = SteamworksClient.GetPingToHost();
					qualityL = SteamworksClient.GetLocalPacketQuality();
					qualityR = SteamworksClient.GetRemotePacketQuality();
					break;
				case NetworkConfig.NetworkTransport.RIPTIDE:
					var metrics = RiptideClient.Client?.Connection?.Metrics;

                    float lossRate = metrics.RollingNotifyLossRate; // 0–1
                    float quality = 1f - lossRate;
                    float remoteQuality = 1f - lossRate;

                    ping = RiptideClient.Client.SmoothRTT;
                    qualityL = quality;
                    qualityR = remoteQuality;
                    break;
			}

			GUILayout.Label($"Ping: {ping}");
            GUILayout.Label($"Quality(L/R): {qualityL:0.00} / {qualityR:0.00}");
			GUILayout.Space(10);
            GUILayout.Label($"Latency: {Utils.NetworkStateToString(NetworkIndicatorsScreen.latencyState)}");
            GUILayout.Label($"Jitter: {Utils.NetworkStateToString(NetworkIndicatorsScreen.jitterState)}");
            GUILayout.Label($"Packet Loss: {Utils.NetworkStateToString(NetworkIndicatorsScreen.packetlossState)}");
            GUILayout.Label($"Server Performance: {Utils.NetworkStateToString(NetworkIndicatorsScreen.serverPerformanceState)}");

            GUILayout.EndScrollView();

			GUI.DragWindow();
		}
	}
}
