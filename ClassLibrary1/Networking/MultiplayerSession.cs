using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Relay.Platforms.Steam;
using ONI_MP.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ONI_MP.Networking
{
    public static class MultiplayerSession
    {
        public static bool ShouldHostAfterLoad = false;

        // Now keyed by string (SteamID or EOS ProductUserId)
        public static readonly Dictionary<string, MultiplayerPlayer> ConnectedPlayers = new Dictionary<string, MultiplayerPlayer>();

        // Local ID as string
        public static string LocalId => SteamUser.GetSteamID().ToString();

        // Host ID as string
        public static string HostId { get; set; } = string.Empty;

        public static bool InSession = false;

        public static bool IsHost => !string.IsNullOrEmpty(HostId) && HostId == LocalId;

        public static bool IsClient => InSession && !IsHost;

        public static readonly Dictionary<string, PlayerCursor> PlayerCursors = new Dictionary<string, PlayerCursor>();

        public static void Clear()
        {
            ConnectedPlayers.Clear();
            HostId = string.Empty;
            DebugConsole.Log("[MultiplayerSession] Session cleared.");
        }

        public static void SetHost(string hostId)
        {
            HostId = hostId;
            DebugConsole.Log($"[MultiplayerSession] Host set to: {hostId} : We're host: {IsHost}");
        }

        public static MultiplayerPlayer GetPlayer(string id)
        {
            return ConnectedPlayers.TryGetValue(id, out var player) ? player : null;
        }

        public static MultiplayerPlayer LocalPlayer => GetPlayer(LocalId);

        public static IEnumerable<MultiplayerPlayer> AllPlayers => ConnectedPlayers.Values;

        public static void CreateNewPlayerCursor(string playerId)
        {
            if (PlayerCursors.ContainsKey(playerId))
                return;

            var canvasGO = GameScreenManager.Instance.ssCameraCanvas;
            if (canvasGO == null)
            {
                DebugConsole.LogError("[MultiplayerSession] ssCameraCanvas is null, cannot create cursor.");
                return;
            }

            var cursorGO = new GameObject($"Cursor_{playerId}");
            cursorGO.transform.SetParent(canvasGO.transform, false);
            cursorGO.layer = LayerMask.NameToLayer("UI");

            var playerCursor = cursorGO.AddComponent<PlayerCursor>();
            playerCursor.AssignPlayer(playerId);
            playerCursor.Init();

            PlayerCursors[playerId] = playerCursor;

            // Steam-specific persona name lookup:
            if (ulong.TryParse(playerId, out var steamIdUlong))
            {
                var steamId = new CSteamID(steamIdUlong);
                DebugConsole.Log($"[MultiplayerSession] Created new cursor for {SteamFriends.GetFriendPersonaName(steamId)}");
            }
            else
            {
                DebugConsole.Log($"[MultiplayerSession] Created new cursor for {playerId}");
            }
        }

        public static void CreateConnectedPlayerCursors()
        {
            // Example: if you're still using Steam lobbies for matchmaking:
            var members = PacketSender.Platform.Lobby.GetAllLobbyMembers();
            foreach (var id in members)
            {
                if (id == LocalId)
                    continue;

                CreateNewPlayerCursor(id);
            }
        }

        public static void RemovePlayerCursor(string playerId)
        {
            if (!PlayerCursors.TryGetValue(playerId, out var cursor))
                return;

            if (cursor != null && cursor.gameObject != null)
            {
                cursor.StopAllCoroutines();
                Object.Destroy(cursor.gameObject);
            }

            PlayerCursors.Remove(playerId);

            if (ulong.TryParse(playerId, out var steamIdUlong))
            {
                var steamId = new CSteamID(steamIdUlong);
                DebugConsole.Log($"[MultiplayerSession] Removed player cursor for {SteamFriends.GetFriendPersonaName(steamId)}");
            }
            else
            {
                DebugConsole.Log($"[MultiplayerSession] Removed player cursor for {playerId}");
            }
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

        public static bool TryGetCursorObject(string playerId, out GameObject cursorGO)
        {
            if (PlayerCursors.TryGetValue(playerId, out var cursor) && cursor != null)
            {
                cursorGO = cursor.gameObject;
                return true;
            }

            cursorGO = null;
            return false;
        }
    }
}
