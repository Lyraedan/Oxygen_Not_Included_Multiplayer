
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
                public static LocString FOUND_X_LOBBIES = "Found {0} joinable lobby(s)";
                public static LocString JOIN_BUTTON = "Join";
                public static LocString FRIEND_ONLY = "Friends Only";
                public static LocString PASSWORD_REQUIRED = "Password Required";
                public static LocString PASSWORD_INCORRECT = "Incorrect password";
                public static LocString CANCEL = "Cancel";
                public static LocString REFRESH = "Refresh";
                public static LocString SEARCH = "{0} Search...";
            }

			public class MODCOMPATIBILITY
			{
				public class MANAGER
				{
					public static LocString WARNING_MOD_HASH_MISMATCH = "Mod hash mismatch - possible mod order difference";
					public static LocString MISSING_MODS = "{0} missing mods";
					public static LocString EXTRA_MODS = "{0} extra mods";
					public static LocString VERSION_MISMATCHES = "{0} version mismatches";
					public static LocString MOD_INCOMPATIBILITY = "Mod incompatibility: {0}";
                }

				public class COMPATIBILITYDIALOG
				{
					public static LocString HEADER_TITLE = "Mod Compatibility Error";
                    public static LocString HEADER_TITLE_ERROR = "Mod Compatibility Error: {0}";
                    public static LocString DISABLE_MODS = "DISABLED MODS (enable these):";
                    public static LocString MISSING_MODS = "MISSING MODS (install these):";
					public static LocString EXTRA_MODS = "EXTRA MODS (disable these):";
					public static LocString VERSION_MISMATCHES = "VERSION MISMATCHES (update these):";
					public static LocString YOU_HAVE_EXTRA_MODS_ALLOWED = "You have extra mods (this is allowed):";
					public static LocString EXTRA_MODS_YOU_HAVE = "EXTRA MODS (you have these):";
					public static LocString ENSURE_HOST_CONFIG = "Please ensure your mods match the host's configuration.";

                    public static LocString BUTTON_INSTALL = "Install";
                    public static LocString BUTTON_INSTALL_ALL = "Install All";
                    public static LocString BUTTON_VIEW = "View";
                    public static LocString BUTTON_UPDATE = "Update";
					public static LocString BUTTON_OK = "Ok";
                    public static LocString BUTTON_ENABLE = "Enable";
                    public static LocString BUTTON_ENABLE_ALL = "Enable All";
                    public static LocString BUTTON_CLOSE = "Close";

                    public static LocString INSTRUCTIONS = "Install/disable the required mods, then try connecting again.";
					public static LocString CONNECTION_ALLOWED = "Connection allowed. Your extra mods shouldn't cause issues.";

                    public static LocString CONNECTION_ALLOWED_HEADER = "Connection Allowed";

					public static LocString YOU_HAVE_EXTRA_MODS = "You have {0} extra mod(s) that the host doesn't have.";
					public static LocString ALLOWED_SHOULDNT_CAUSE_ISSUES = "This is allowed and shouldn't cause issues.";
					public static LocString YOUR_EXTRA_MODS = "Your extra mods:";
					public static LocString MOD_ENTRY = "• {0}";

					public static LocString MODS_ENABLED_SUCCESSFULLY = "Mods enabled successfully!\nPlease restart the game for changes to take effect.";
					public static LocString ALL_MODS_ENABLED = "All mods have been enabled!";
					public static LocString RESTART = "Close this window to restart the game and apply changes.";
					public static LocString MODS_CHANGED = "Mods have been enabled. Close this window to restart the game.";

					public static LocString INSTALL_DISABLE_RECONNECT = "Install/disable mods, then reconnect.\nPress ESC to close.";
                }

                public class COMPATIBILITYRESULT
				{
					public static LocString APPROVED = "Approved";
                    public static LocString APPROVED_EXTRA = "Compatible (client has {0} extra mod(s))";
                    public static LocString APPROVED_COMPATIBLE = "Compatible";
                    public static LocString REJECT_NOT_A_HOST = "Internal error: not a host";
					public static LocString REJECT_GAME_VERSION_MISMATCH = "Game version mismatch: Host={0}, Client={1}";
					public static LocString COMPATIBILITY_ISCOMPATIBLE = "Compatible: {0}";
                    public static LocString COMPATIBILITY_INCOMPATIBLE = "Incompatible: {0} ({1})";
                    public static LocString COMPATIBILITY_MISSING = "{0} missing";
                    public static LocString COMPATIBILITY_EXTRA = "{0} extra";
                    public static LocString COMPATIBILITY_MISMATCH = "{0} version mismatches";
					public static LocString REJECT_VERIFICATION_ERROR = "Verification error: {0}";
                }

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
                public static LocString CHAT_CLIENT_REJECTED = "<color=red>[System]</color> {0} was rejected due to mod incompatibility: {1}";
				public static LocString CHAT_CLIENT_JOINED = "<color=yellow>[System]</color> <b>{0}</b> joined the game.";
                public static LocString CHAT_CLIENT_LEFT = "<color=yellow>[System]</color> <b>{0}</b> left the game.";

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

					public static LocString DOWNLOADING_SAVE_FILE = "Downloading Save File\n\n{0} {1}%\n({2}/{3} chunks)";
					public static LocString DOWNLOAD_COMPLETE = "Download Complete!\n\n{0} 100%\n({1}/{2} chunks)\n\nLoading world...";

                }
				public class SYNC
				{
					public static LocString HARDSYNC_INPROGRESS = "Hard sync in progress!";
					public static LocString FINALIZING_SYNC = "All players are ready!\nPlease wait...";
					public static LocString WAITING_FOR_PLAYERS_SYNC = "Waiting for players ({0}/{1} ready)...\n";

					// New world sync menu
                    public static LocString CLIENT_SYNC_PROGRESS = "Client Sync Progress";
                    public static LocString CLIENT_PROGRESS = "{0}: {1} {2}%";
                    public static LocString CLIENT_CHUNK_SYNC_DATA = "({0}/{1})";
                    public static LocString CLIENT_SYNC_COMPLETE = "[COMPLETE]";
                    public static LocString ALL_CLIENTS_SYNCED = "ALL CLIENTS SYNCHRONIZED!\nWAITING FOR THEM TO LOAD!";

					public static LocString PROGRESS_BAR_FILLED = "|";
					public static LocString PROGRESS_BAR_EMPTY = " ";
					public static LocString PROGRESS_BAR = "[{0}]";

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
