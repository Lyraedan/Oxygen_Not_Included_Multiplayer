using HarmonyLib;
using KMod;
using ONI_MP.Components;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ONI_MP
{
	//Template: https://github.com/O-n-y/OxygenNotIncludedModTemplate

	public class MultiplayerMod : UserMod2
	{

		public static readonly Dictionary<string, AssetBundle> LoadedBundles = new Dictionary<string, AssetBundle>();

		public static System.Action OnPostSceneLoaded;

		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
            string logPath = System.IO.Path.Combine(Application.dataPath, "../ONI_MP_Log.txt");

			try
			{
				DebugConsole.Init(); // Init console first to catch logs
				PacketTracker.Init();
				DebugConsole.Log("[ONI_MP] Loaded Oxygen Not Included Together Multiplayer Mod.");

                PacketRegistry.RegisterDefaults();

                // CHECKPOINT 1
                System.IO.File.AppendAllText(logPath, "[Trace] Checkpoint 1: Pre-DebugMenu\n");
				DebugMenu.Init();
				NetworkStatisticsMenu.Init();

				// CHECKPOINT 2
				System.IO.File.AppendAllText(logPath, "[Trace] Checkpoint 2: Pre-SteamLobby\n");
				SteamLobby.Initialize();

				// CHECKPOINT 3
				System.IO.File.AppendAllText(logPath, "[Trace] Checkpoint 3: Pre-GameObjects\n");
				var go = new GameObject("Multiplayer_Modules");
				UnityEngine.Object.DontDestroyOnLoad(go);

				// CHECKPOINT 4
				System.IO.File.AppendAllText(logPath, "[Trace] Checkpoint 4: Pre-Components\n");
				go.AddComponent<SteamNetworkingComponent>();
				go.AddComponent<UIVisibilityController>();
				go.AddComponent<MainThreadExecutor>();
				go.AddComponent<CursorManager>();
				go.AddComponent<BuildingSyncer>();
				go.AddComponent<WorldStateSyncer>();

				// CHECKPOINT 5
				System.IO.File.AppendAllText(logPath, "[Trace] Checkpoint 5: Pre-Listeners\n");
				SetupListeners();

				// CHECKPOINT 6
				System.IO.File.AppendAllText(logPath, "[Trace] Checkpoint 6: Pre-ResLoad\n");
				LoadAssetBundles();

				foreach (var res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
				{
					DebugConsole.Log("Embedded Resource: " + res);
				}

				System.IO.File.AppendAllText(logPath, "[Trace] Checkpoint 7: Success\n");
			}
			catch (Exception ex)
			{
				DebugConsole.LogError($"[ONI_MP] CRITICAL ERROR IN ONLOAD: {ex.Message}");
				DebugConsole.LogException(ex);
			}
		}

		void LoadAssetBundles()
		{
			// Load custom asset bundles
			string cursor_bundle = GetBundleBasedOnPlatform("ONI_MP.Assets.bundles.playercursor_win.bundle",
																											"ONI_MP.Assets.bundles.playercursor_mac.bundle",
																											"ONI_MP.Assets.bundles.playercursor_lin.bundle");
			LoadAssetBundle("playercursorbundle", cursor_bundle);
		}

		private void SetupListeners()
		{
			App.OnPostLoadScene += () =>
			{
				OnPostSceneLoaded?.Invoke();
			};

			ReadyManager.SetupListeners();
		}
		public static AssetBundle LoadAssetBundle(string bundleKey, string resourceName)
		{
			if (LoadedBundles.TryGetValue(bundleKey, out var bundle))
			{
				DebugConsole.Log($"LoadAssetBundle: Reusing cached AssetBundle '{bundleKey}'.");
				return bundle;
			}

			// load with your existing loader
			bundle = ResourceLoader.LoadEmbeddedAssetBundle(resourceName);

			if (bundle != null)
			{
				LoadedBundles[bundleKey] = bundle;
				DebugConsole.Log($"LoadAssetBundle: Successfully loaded AssetBundle '{bundleKey}' from resource '{resourceName}'.");

				foreach (var name in bundle.GetAllAssetNames())
				{
					DebugConsole.Log($"[ONI_MP] Bundle Asset: {name}");
				}

				foreach (var name in bundle.GetAllScenePaths())
				{
					DebugConsole.Log($"[ONI_MP] Scene: {name}");
				}

				foreach (var name in bundle.GetAllAssetNames())
				{
					DebugConsole.Log($"[ONI_MP] Asset: {name}");
				}
				return bundle;
			}
			else
			{
				DebugConsole.LogError($"LoadAssetBundle: Could not load AssetBundle from resource '{resourceName}'");
				return null;
			}
		}

		public string GetBundleBasedOnPlatform(string windows_bundle, string mac_bundle, string linux_bundle)
		{
			switch (Application.platform)
			{
				case RuntimePlatform.OSXPlayer:
					return mac_bundle;
				case RuntimePlatform.LinuxPlayer:
					return linux_bundle;
				default:
					return windows_bundle;
			}
		}
	}
}
