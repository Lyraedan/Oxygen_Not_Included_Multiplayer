using HarmonyLib;
using ONI_MP.Networking;

//[HarmonyPatch(typeof(LoadingOverlay), nameof(LoadingOverlay.Load))]
//public static class LoadingOverlayPatch
//{
//    [HarmonyPostfix]
//    public static void Load_Postfix()
//    {

//        var overlay = UnityEngine.Object.FindObjectOfType<LoadingOverlay>();
//        if (overlay == null) return;

//        var locText = overlay.GetComponentInChildren<LocText>();
//        if (locText == null) return;

//        bool isMultiplayer = MultiplayerSession.ShouldHostAfterLoad /* host after load? */
//                              || SteamLobby.InLobby         /* you're in a lobby */;

//        string multiplayer_message = MultiplayerSession.IsHost ? "Hosting game..." : "Joining game...";

//        locText.SetText(isMultiplayer
//            ? multiplayer_message
//            : "Loading...");
//    }
//}
