
using Steamworks;

namespace ONI_MP
{
	internal class MP_STRINGS
	{
		public class UI
		{
			public class SERVERBROWSER
			{
                public static LocString MULTIPLAYER_SESSION_TITLE = "Multiplayer Session";
				public static LocString LOBBY_CODE = "Lobby Code:";
				public static LocString COPY = "Copy";
                public static LocString COPIED = "Copied!";
				public static LocString CONNECTED_PLAYERS = "Connected Players: {0}";
				public static LocString TITLE = "Public Lobby Browser";


                public class HEADERS
				{
					public static LocString COLONY = "Colony";
                    public static LocString HOST = "Host";
                    public static LocString PLAYERS = "Players";
                    public static LocString CYCLE = "Cycle";
                    public static LocString DUPES = "Dupes";
                    public static LocString PING = "Ping";
                }

                public static LocString JOIN_BY_CODE = "Join by Code";
                public static LocString BACK = "Back";
                public static LocString LOADING_LOBBIES = "Loading Lobbies...";
                public static LocString NO_PUBLIC_LOBBIES_FOUND = "No public lobbies found. Try hosting your own!";
                public static LocString FOUND_X_LOBBIES = "Found {0} lobby(s)";
                public static LocString JOIN_BUTTON = "Join";
				public static LocString PASSWORD_REQUIRED = "Password Required";
                public static LocString PASSWORD_INCORRECT = "Incorrect password";
                public static LocString CANCEL = "Cancel";
                public static LocString REFRESH = "Refresh";
                public static LocString SEARCH = "{0} Search...";
            }

            public class HOSTLOBBYCONFIGSCREEN
			{
				public static LocString HOST_LOBBY_SETTINGS = "Host Lobby Settings";
                public static LocString LOBBY_VISIBILITY = "Lobby Visibility:";
				public static LocString LOBBY_VISIBILITY_PUBLIC = "Public";
				public static LocString LOBBY_VISIBILITY_FRIENDSONLY = "Friends Only";
				public static LocString PASSWORD_TITLE = "Password (optional):";
				public static LocString PASSWORD_NOTE = "Leave empty for no password";
                public static LocString CONTINUE = "Continue";
                public static LocString CANCEL = "Cancel";
				public static LocString LOBBY_SIZE = "Lobby Size:";
                public static LocString HARD_SYNC = "Hard sync after each cycle:";
                public static LocString HARD_SYNC_ENABLED = "Enabled";
                public static LocString HARD_SYNC_DISABLED = "Disabled";
            }

            public class JOINBYDIALOGMENU
			{
				public static LocString JOIN_BY_CODE = "Join By Code";
                public static LocString ENTER_LOBBY_CODE = "Enter Lobby Code:";
				public static LocString DEFAULT_CODE = "XXXX-XXXX";
				public static LocString PASSWORD_REQUIRED = "Password Required:";
                public static LocString ENTER_PASSWORD = "Enter password";
				public static LocString JOIN = "Join";
				public static LocString CANCEL = "Cancel";

				public static LocString ERR_ENTER_CODE = "Please enter a lobby code";
                public static LocString ERR_INVALID_CODE = "Invalid lobby code format";
                public static LocString ERR_PARSE_CODE_FAILED = "Could not parse lobby code";

				public static LocString CHECKING_LOBBY = "Checking lobby...";
				public static LocString LOBBY_REQUIRES_PASSWORD = "This lobby requires a password";

				public static LocString VALIDATE_ENTER_PASSWORD = "Please enter the password";
                public static LocString VALIDATE_ERR_INCORRECT_PASSWORD = "Incorrect password";

				public static LocString JOINING = "Joining...";
            }

            // Main Menu multiplayer menu
            public class MULTIPLAYERMENU
			{
				public static LocString TITLE = "Multiplayer";
				public static LocString HOST_WORLD = "Host World";
				public static LocString HOST_WORLD_FLAVOR = "Select a save to host";
				public static LocString BROWSE_LOBBIES = "Browse Lobbies";
				public static LocString BROWSE_LOBBIES_FLAVOR = "Find public games to join";
				public static LocString JOIN_BY_CODE = "Join by code";
				public static LocString JOIN_BY_CODE_FLAVOR = "Enter a lobby code";
				public static LocString JOIN_BY_STEAM = "Join via Steam";
				public static LocString JOIN_BY_STEAM_FLAVOR = "Find friends playing";
                public static LocString BACK = "Back";
            }

            public class MAINMENU
			{
				public static LocString JOINGAME = "JOIN GAME";
				public static LocString DISCORD_INFO = "Join ONI Together\non Discord";

                public class MULTIPLAYER
                {
                    public static LocString LABEL = "MULTIPLAYER";
                }
            }
			public class PAUSESCREEN
			{
				public class MULTIPLAYER
				{
					public static LocString LABEL = "Multiplayer";
				}

				public class HARDSYNC
				{
					public static LocString LABEL = "Hard sync after each cycle:";
				}

				//maybe add tooltips to these later in some way?
				public class HOSTGAME
				{
					public static LocString LABEL = "Host Game";
					//e.g:
					//public static LocString TOOLTIP = "Host your current game as a multiplayer session";
				}
				public class INVITE
				{
					public static LocString LABEL = "Invite Friends";
				}
				public class DOHARDSYNC
				{
					public static LocString LABEL = "Perform Hard Sync";
				}
				public class HARDSYNCNOTAVAILABLE
				{
					public static LocString LABEL = "Already hard synced this cycle!";
				}
				public class ENDSESSION
				{
					public static LocString LABEL = "End Session";
				}
				public class LEAVESESSION
				{
					public static LocString LABEL = "Leave Session";
				}
			}
			public class MP_CHATWINDOW
			{
				public static LocString CHAT_INITIALIZED = "<color=yellow>System:</color> Chat initialized.";
				public class RESIZE
				{
					public static LocString EXPAND = "Chat (+)";
					public static LocString RETRACT = "Chat (-)";
				}
			}
			public class MP_OVERLAY
			{
				public class HOST
				{
					public static LocString STARTINGHOSTING = "Hosting game...";
				}
				public class CLIENT
				{
					public static LocString DOWNLOADING_GAME = "Downloading world: {0}%";
					public static LocString LOST_CONNECTION = "Connection to the host was lost!";
					public static LocString MISSING_SAVE_FILE = "Downloaded save file not found.";
					public static LocString CONNECTING_TO_HOST = "Connecting to {0}!";
					public static LocString WAITING_FOR_PLAYER = "Waiting for {0}...";
				}
				public class SYNC
				{
					public static LocString HARDSYNC_INPROGRESS = "Hard sync in progress!";
					public static LocString FINALIZING_SYNC = "All players are ready!\nPlease wait...";
					public static LocString WAITING_FOR_PLAYERS_SYNC = "Waiting for players ({0}/{1} ready)...\n";
					public class READYSTATE
					{
						public static LocString READY = "Ready";
						public static LocString UNREADY = "Loading";
						public static LocString UNKNOWN = "Unknown";
					}
				}
			}
		}
	}
}
