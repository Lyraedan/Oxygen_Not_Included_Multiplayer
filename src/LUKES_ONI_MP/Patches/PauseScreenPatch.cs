using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace ONI_MP.Patches
{
    [HarmonyPatch]
    public static class PauseScreenPatch
    {
        // This method is called when "Quit" is confirmed in the pause menu
        [HarmonyPatch(typeof(PauseScreen), "OnQuitConfirm")]
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void OnQuitConfirm_Prefix(bool saveFirst)
        {
            if (MultiplayerSession.InSession)
            {
                SteamLobby.LeaveLobby();
                MultiplayerSession.Clear();
            }
        }

        // This prevents the game from pausing when the PauseScreen opens in multiplayer
        [HarmonyPatch(typeof(SpeedControlScreen), nameof(SpeedControlScreen.Pause))]
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool PreventPauseInMultiplayer(bool playSound = true, bool isCrashed = false)
        {
            // Restore pause functionality
            /*if (MultiplayerSession.InSession && !isCrashed)
            {
                return false;
            }*/

            return true;
        }

        [HarmonyPatch(typeof(PauseScreen), "ConfigureButtonInfos")]
        public static class PauseScreen_AddInviteButton
        {
            static void Postfix(PauseScreen __instance)
            {
                // Only in multiplayer
                if (!MultiplayerSession.InSession) return; // Do we want clients to be able to invite people???

                var buttonsField = AccessTools.Field(typeof(KModalButtonMenu), "buttons");
                var buttonInfos = ((KModalButtonMenu.ButtonInfo[])buttonsField.GetValue(__instance))?.ToList()
                                  ?? new List<KModalButtonMenu.ButtonInfo>();

                if (buttonInfos.Any(b => b.text == "Invite"))
                    return;

                // Insert after "Resume"
                int idx = buttonInfos.FindIndex(b => b.text == "Resume") + 1;
                if (idx <= 0) idx = 1;

                buttonInfos.Insert(idx, new KModalButtonMenu.ButtonInfo(
                    "Invite",
                    new UnityAction(() => {
                        SteamFriends.ActivateGameOverlayInviteDialog(MultiplayerSession.HostSteamID);
                    })
                ));

                buttonsField.SetValue(__instance, buttonInfos.ToArray());
            }
        }
    }
}
