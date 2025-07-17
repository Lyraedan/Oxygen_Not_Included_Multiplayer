using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking;
using ONI_MP.SharedStorage;
using UnityEngine;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch]
    public static class SaveLoaderPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoader), "OnSpawn")]
        public static void Postfix_OnSpawn()
        {
            TryCreateLobbyAfterLoad("[Multiplayer] Lobby created after world load.");
            if(MultiplayerSession.InSession)
            {
                SpeedControlScreen.Instance?.Unpause(false); // Unpause the game
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoader), nameof(SaveLoader.LoadFromWorldGen))]
        public static void Postfix_LoadFromWorldGen(bool __result)
        {
            if (__result)
                TryCreateLobbyAfterLoad("[Multiplayer] Lobby created after new world gen.");
        }

        private static void TryCreateLobbyAfterLoad(string logMessage)
        {
            DebugConsole.Log($"[SaveLoaderPatch] TryCreateLobbyAfterLoad called. ShouldHostAfterLoad: {MultiplayerSession.ShouldHostAfterLoad}");
            
            if (MultiplayerSession.ShouldHostAfterLoad)
            {
                MultiplayerSession.ShouldHostAfterLoad = false;
                DebugConsole.Log("[SaveLoaderPatch] Proceeding with lobby creation...");

                // Check if storage is initialized, if not wait for it
                if (!SharedStorageManager.Instance.IsInitialized)
                {
                    DebugConsole.Log("[SaveLoaderPatch] Waiting for shared storage to initialize before creating lobby...");
                    
                    // Subscribe to the initialization event
                    SharedStorageManager.Instance.OnInitialized.AddListener(() =>
                    {
                        DebugConsole.Log("[SaveLoaderPatch] Shared storage initialized, creating lobby now...");
                        CreateLobbyNow(logMessage);
                    });
                    return;
                }

                CreateLobbyNow(logMessage);
            }
            else
            {
                DebugConsole.Log("[SaveLoaderPatch] ShouldHostAfterLoad is false, not creating lobby");
            }
        }

        private static void CreateLobbyNow(string logMessage)
        {
            DebugConsole.Log("[SaveLoaderPatch] CreateLobbyNow called - creating Steam lobby...");
            SteamLobby.CreateLobby(onSuccess: () =>
            {
                SpeedControlScreen.Instance?.Unpause(false);
                DebugConsole.Log(logMessage);
                DebugConsole.Log("[SaveLoaderPatch] Lobby creation success callback executed");
            });
        }
    }
}
