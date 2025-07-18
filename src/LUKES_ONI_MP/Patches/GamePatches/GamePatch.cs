using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc.World;
using ONI_MP.Misc.Research;
using ONI_MP.Misc.DuplicantBehavior;
using ONI_MP.Networking;
using UnityEngine;
using static STRINGS.UI;

namespace ONI_MP.Patches.GamePatches
{
    // This single class contains BOTH patches.
    public static class GamePatch
    {
        // Patch Game.Update to run the two batchers if host
        [HarmonyPatch(typeof(Game), "Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix()
        {
            if (MultiplayerSession.IsHost)
            {
                InstantiationBatcher.Update();
                WorldUpdateBatcher.Update();
                EnvironmentalSystemsManager.Update(); // Environmental Systems: gas flow, pressure, temperature, fluid dynamics
                ResearchSkillsManager.Update(); // Research & Skills: research progress, skill points, technology unlocks
                // DuplicantBehaviorManager.Update(); // Duplicant Behavior: work assignments, pathfinding, idle/sleep/stress behaviors
                // Note: DuplicantBehaviorManager uses coroutines and doesn't need Update() call
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        [HarmonyPostfix]
        public static void OnSpawnPostfix()
        {
            DebugConsole.Log($"[GamePatch] Game OnSpawn called. ShouldHostAfterLoad: {MultiplayerSession.ShouldHostAfterLoad}");
            
            // Check if we should create a lobby after loading
            if (MultiplayerSession.ShouldHostAfterLoad)
            {
                DebugConsole.Log("[GamePatch] Proceeding with lobby creation...");
                MultiplayerSession.ShouldHostAfterLoad = false; // Reset the flag
                
                DebugConsole.Log("[GamePatch] CreateLobbyNow called - creating Steam lobby...");
                SteamLobby.CreateLobby(onSuccess: () => {
                    DebugConsole.Log("[Multiplayer] Lobby created after world load.");
                    DebugConsole.Log("[GamePatch] Lobby creation success callback executed");
                });
            }
            
            // Initialize behavior systems when game starts
            if (MultiplayerSession.InSession)
            {
                DuplicantBehaviorManager.Initialize();
                DebugConsole.Log("[GamePatch] Duplicant behavior synchronization initialized");
            }
        }
    }
}
