using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking;
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
            if (MultiplayerSession.ShouldHostAfterLoad)
            {
                MultiplayerSession.ShouldHostAfterLoad = false;

                SteamLobby.CreateLobby(onSuccess: () =>
                {
                    SpeedControlScreen.Instance?.Unpause(false);
                    DebugConsole.Log(logMessage);
                });
            }
        }
    }
}
