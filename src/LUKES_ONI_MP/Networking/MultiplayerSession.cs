using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;
using ONI_MP.UI;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Networking
{
    public static class MultiplayerSession
    {
        public static bool ShouldHostAfterLoad = false;

        public static readonly Dictionary<CSteamID, MultiplayerPlayer> ConnectedPlayers = new Dictionary<CSteamID, MultiplayerPlayer>();

        public static CSteamID LocalSteamID => SteamUser.GetSteamID();

        public static CSteamID HostSteamID { get; set; } = CSteamID.Nil;

        public static bool InSession = false;

        public static bool IsHost => HostSteamID == LocalSteamID;

        public static bool IsClient => InSession && !IsHost;

        public static readonly Dictionary<CSteamID, PlayerCursor> PlayerCursors = new Dictionary<CSteamID, PlayerCursor>();

        public static void Clear()
        {
            ConnectedPlayers.Clear();
            HostSteamID = CSteamID.Nil;
            DebugConsole.Log("[MultiplayerSession] Session cleared.");
        }

        public static void SetHost(CSteamID host)
        {
            HostSteamID = host;
            DebugConsole.Log($"[MultiplayerSession] Host set to: {host}");
        }

        public static MultiplayerPlayer GetPlayer(CSteamID id)
        {
            return ConnectedPlayers.TryGetValue(id, out var player) ? player : null;
        }

        public static MultiplayerPlayer LocalPlayer => GetPlayer(LocalSteamID);

        public static IEnumerable<MultiplayerPlayer> AllPlayers => ConnectedPlayers.Values;

        public static void CreateNewPlayerCursor(CSteamID steamID)
        {
            if (PlayerCursors.ContainsKey(steamID))
                return;

            var canvasGO = GameScreenManager.Instance.ssCameraCanvas;
            if (canvasGO == null)
            {
                DebugConsole.LogError("[MultiplayerSession] ssCameraCanvas is null, cannot create cursor.");
                return;
            }

            var cursorGO = new GameObject($"Cursor_{steamID}");
            cursorGO.transform.SetParent(canvasGO.transform, false);
            cursorGO.layer = LayerMask.NameToLayer("UI");

            var playerCursor = cursorGO.AddComponent<PlayerCursor>();

            playerCursor.AssignPlayer(steamID);
            playerCursor.Init();

            PlayerCursors[steamID] = playerCursor;
            DebugConsole.Log($"[MultiplayerSession] Created new cursor for {SteamFriends.GetFriendPersonaName(steamID)}");
        }

        public static void CreateConnectedPlayerCursors()
        {
            var members = SteamLobby.GetAllLobbyMembers();
            foreach (var playerId in members)
            {
                if (playerId == LocalSteamID)
                    continue;

                CreateNewPlayerCursor(playerId);
            }
        }

        public static void RemovePlayerCursor(CSteamID steamID)
        {
            if (!PlayerCursors.TryGetValue(steamID, out var cursor))
                return;

            if (cursor != null && cursor.gameObject != null)
            {
                cursor.StopAllCoroutines();
                Object.Destroy(cursor.gameObject);
            }

            PlayerCursors.Remove(steamID);
            DebugConsole.Log($"[MultiplayerSession] Removed player cursor for {SteamFriends.GetFriendPersonaName(steamID)}");
        }

        public static void RemoveAllPlayerCursors()
        {
            foreach (var kvp in PlayerCursors)
            {
                var cursor = kvp.Value;
                if (cursor != null && cursor.gameObject != null)
                {
                    cursor.StopAllCoroutines();
                    Object.Destroy(cursor.gameObject);
                }
            }

            PlayerCursors.Clear();
            DebugConsole.Log("[MultiplayerSession] Removed all player cursors.");
        }

        public static bool TryGetCursorObject(CSteamID steamID, out GameObject cursorGO)
        {
            if (PlayerCursors.TryGetValue(steamID, out var cursor) && cursor != null)
            {
                cursorGO = cursor.gameObject;
                return true;
            }

            cursorGO = null;
            return false;
        }


    }
}
