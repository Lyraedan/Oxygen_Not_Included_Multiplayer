using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using System.Reflection;

namespace ONI_MP.Patches.GamePatches
{
    [HarmonyPatch]
    public static class SaveLoaderPatch
    {
        // Patch the method that gets called after a save file is loaded
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return typeof(Game).GetMethod(
                "OnSpawn",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
        }

        [HarmonyPostfix]
        public static void Postfix_GameOnSpawn()
        {
            DebugConsole.Log($"[SaveLoaderPatch] Game OnSpawn called. ShouldHostAfterLoad: {MultiplayerSession.ShouldHostAfterLoad}");
            
            if (MultiplayerSession.ShouldHostAfterLoad)
            {
                DebugConsole.Log("[SaveLoaderPatch] Proceeding with lobby creation...");
                MultiplayerSession.ShouldHostAfterLoad = false; // Reset the flag
                
                CreateLobbyNow();
            }
        }

        private static void CreateLobbyNow()
        {
            DebugConsole.Log("[SaveLoaderPatch] CreateLobbyNow called - creating Steam lobby...");
            
            SteamLobby.CreateLobby(onSuccess: () => {
                DebugConsole.Log("[Multiplayer] Lobby created after world load.");
                DebugConsole.Log("[SaveLoaderPatch] Lobby creation success callback executed");
            });
        }
    }
}
