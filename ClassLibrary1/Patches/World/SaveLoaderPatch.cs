using System;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;

namespace ONI_MP.Patches.World
{
	[HarmonyPatch]
	public static class SaveLoaderPatch
	{

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SaveLoader), nameof(SaveLoader.LoadFromWorldGen))]
		public static void Postfix_LoadFromWorldGen(bool __result)
		{
			if (__result)
				TryCreateLobbyAfterLoad("[Multiplayer] Lobby created after new world gen.");
		}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoader),
					  nameof(SaveLoader.Load),
					  new Type[] { typeof(IReader) })]
        public static void Postfix(IReader reader, ref bool __result)
        {
            // __result == true means the save loaded successfully
            if (!__result)
                return;

            OnSaveLoaded();
        }

        private static void OnSaveLoaded()
        {
            // Your logic here
            TryCreateLobbyAfterLoad("[Multiplayer] Lobby created after world load.");
            if (MultiplayerSession.InSession)
            {
                SpeedControlScreen.Instance?.Unpause(false); // Unpause the game
            }
            //ReadyManager.SendReadyStatusPacket(Networking.States.ClientReadyState.Ready);
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
