
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

            public class MODCOMPATIBILITY
			{
				public static LocString TITLE = "Mod Compatibility Settings";
				public static LocString ENABLE_VERIFICATION = "Enable Mod Verification";
				public static LocString ENABLE_VERIFICATION_TOOLTIP = "Check if all players have the same mods before allowing connection";
				public static LocString ALLOW_VERSION_MISMATCHES = "Allow Version Mismatches";
				public static LocString ALLOW_VERSION_MISMATCHES_TOOLTIP = "Allow players to join with different mod versions";
				public static LocString ALLOW_EXTRA_MODS = "Allow Extra Mods";
				public static LocString ALLOW_EXTRA_MODS_TOOLTIP = "Allow players to have additional mods that the host doesn't have";

				public class POPUP
				{
					public static LocString TITLE = "Mod Compatibility Error";
					public static LocString MISSING_MODS_HEADER = "Missing Required Mods:";
					public static LocString MISSING_MODS_SECTION = "MISSING MODS (install these):";
					public static LocString DISABLED_MODS_SECTION = "DISABLED MODS (enable these):";
					public static LocString EXTRA_MODS_HEADER = "Extra Mods (not allowed):";
					public static LocString EXTRA_MODS_SECTION = "EXTRA MODS (you have these):";
					public static LocString VERSION_MISMATCH_HEADER = "Version Mismatches:";
					public static LocString VERSION_MISMATCH_SECTION = "VERSION MISMATCHES (update these):";
					public static LocString INSTALL_ALL = "Install All Missing Mods";
					public static LocString INSTALL = "Install";
					public static LocString ENABLE_ALL = "Enable All Disabled Mods";
					public static LocString ENABLE = "Enable";
					public static LocString VIEW = "View";
					public static LocString UPDATE = "Update";
					public static LocString VIEW_WORKSHOP = "View on Workshop";
					public static LocString CLOSE = "Close";
					public static LocString INSTALLING = "Installing mods...";
					public static LocString INSTALL_PROGRESS = "Installing mod {0} of {1}...";
					public static LocString INSTALL_COMPLETE = "Installation complete! Please restart the game.";
					public static LocString INSTALL_FAILED = "Installation failed for some mods. Please try manually.";
					public static LocString ENABLING_MODS = "Enabling mods...";
					public static LocString RESTART_REQUIRED = "Restart required to apply mod changes.";
					public static LocString PLEASE_WAIT = "Please wait while mods are being downloaded and installed...";
					public static LocString ALL_MODS_ENABLED = "All mods have been enabled!";
					public static LocString CLOSE_TO_RESTART = "Close this window to restart the game and apply changes.";
					public static LocString MODS_ENABLED_CLOSE_TO_RESTART = "Mods have been enabled. Close this window to restart the game.";
					public static LocString PREPARING_INSTALL = "Preparing to install mods...";
					public static LocString ACTIVATING_MODS = "Activating installed mods...";
					public static LocString INSTALL_SUCCESS = "Successfully installed and activated {0} mods!";
					public static LocString INSTALL_SUCCESS_SINGLE = "Successfully installed and activated {0}!";
					public static LocString INSTALL_PARTIAL_SUCCESS = "Installed {0} mods. Some may need manual activation or game restart.";
					public static LocString INSTALL_PARTIAL_SUCCESS_SINGLE = "Installed {0}. May need manual activation or game restart.";
					public static LocString INSTALL_FAILED_SINGLE = "Installation failed for {0}: {1}";
					public static LocString INSTALL_FAILED_GENERIC = "Installation failed: {0}";
					public static LocString INSTALL_NO_MODS_PROCESSED = "Installation completed but no mods were processed.";
					public static LocString INSTALLING_SINGLE = "Installing {0}...";
					public static LocString INSTALLING_PROGRESS_DETAILED = "Installing mod {0} of {1}...";
					public static LocString ACTIVATING_MOD = "Activating mod...";
					public static LocString MODS_ENABLED_RESTART_NOTIFICATION = "Mods enabled successfully!\nPlease restart the game for changes to take effect.";
					public static LocString EXTRA_MODS_INFO = "You have extra mods (this is allowed):";
					public static LocString INSTALL_DISABLE_INSTRUCTION = "Install/disable the required mods, then try connecting again.";
					public static LocString CONNECTION_ALLOWED_INFO = "Connection allowed. Your extra mods shouldn't cause issues.";
					public static LocString FAILED_INSTALL_ERROR = "Failed to install {0}: {1}";

					// Apply confirmation dialog strings
					public static LocString APPLY = "Apply";
					public static LocString CANCEL = "Cancel";
					public static LocString CHANGES_APPLIED_TITLE = "Changes Applied";
					public static LocString ACTIVATED_MODS_SECTION = "Activated Mods:";
					public static LocString DEACTIVATED_MODS_SECTION = "Deactivated Mods:";
					public static LocString ONE_MOD_ACTIVATED = "1 mod was activated";
					public static LocString ONE_MOD_DEACTIVATED = "1 mod was deactivated";
					public static LocString MULTIPLE_MODS_ACTIVATED = "{0} mods were activated";
					public static LocString MULTIPLE_MODS_DEACTIVATED = "{0} mods were deactivated";
					public static LocString MULTIPLE_MODS_MODIFIED = "{0} mods were modified";
					public static LocString GAME_RESTART_REQUIRED_MESSAGE = "A game restart is required to apply these changes.";

					// Restart prompt strings (ItsLuke feedback: native restart prompt)
					public static LocString DISABLE = "Disable";
					public static LocString RESTART_REQUIRED_TITLE = "Game Restart Required";
					public static LocString RESTART_REQUIRED_MESSAGE = "Mods have been enabled or disabled. The game needs to restart to apply these changes.\n\nWould you like to restart now?";
					public static LocString RESTART_NOW = "Restart Now";
					public static LocString RESTART_LATER = "Restart Later";
				}

				// Restart prompt strings (outside POPUP for general use)
				public static LocString RESTART_REQUIRED_TITLE = "Game Restart Required";
				public static LocString RESTART_REQUIRED_MESSAGE = "Mod changes require a game restart to take effect.";
				public static LocString RESTART_NOW = "Restart Now";
				public static LocString RESTART_LATER = "Restart Later";
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
