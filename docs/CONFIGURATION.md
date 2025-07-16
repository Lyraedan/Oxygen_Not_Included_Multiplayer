# ONI Multiplayer Mod Configuration Guide

This document explains all the configuration options available in the `multiplayer_settings.json` file.

## File Location
The configuration file should be placed in the same directory as the mod's DLL file:
- `multiplayer_settings.json`

## Cloud Storage Options

The mod supports two cloud storage providers for save file synchronization:

### Steam Cloud (Default)
- **No setup required** - Uses your Steam account automatically
- **100MB storage limit per user**
- **Automatic authentication** through Steam
- **File versioning** and conflict resolution
- **Works offline** (local storage with sync when online)

### Google Drive
- **Requires setup** - Need to configure credentials.json
- **15GB free storage** (Google account)
- **Large file support** - Can handle saves larger than 100MB
- **Manual authentication** required
- **Setup guide**: [Google Drive Setup Guide](https://github.com/Lyraedan/Oxygen_Not_Included_Multiplayer/wiki/Google-Drive-Setup-Guide)

### Switching Providers
To switch between providers, change the `Provider` setting in your configuration:
- For Steam Cloud: `"Provider": "SteamCloud"`
- For Google Drive: `"Provider": "GoogleDrive"`

## Configuration Structure

### Host Settings
These settings control the behavior when hosting a multiplayer session.

#### `MaxLobbySize` (integer, default: 4)
- Maximum number of players allowed in a lobby, including the host
- Range: 1-10 (recommended maximum of 8 for performance)
- Example: `"MaxLobbySize": 6`

#### `MaxMessagesPerPoll` (integer, default: 128)
- Maximum number of network messages to process per polling cycle
- Higher values provide better performance but use more CPU
- Range: 16-512
- Example: `"MaxMessagesPerPoll": 256`

#### `SaveFileTransferChunkKB` (integer, default: 256)
- Size of save file transfer chunks in kilobytes
- Smaller chunks = more reliable transfer over slow connections
- Larger chunks = faster transfer over good connections
- Range: 64-1024
- Example: `"SaveFileTransferChunkKB": 512`

#### `CloudStorage` Settings
Settings for cloud storage integration (save file sharing).

##### `Provider` (string, default: "SteamCloud")
- The cloud storage provider to use for save file synchronization
- Options: "SteamCloud" or "GoogleDrive"
- SteamCloud: Uses Steam's built-in cloud storage (no setup required)
- GoogleDrive: Uses Google Drive API (requires credentials setup)
- Example: `"Provider": "GoogleDrive"`

##### `GoogleDrive` Settings (only used when Provider is "GoogleDrive")
###### `ApplicationName` (string, default: "ONI Multiplayer Mod")
- Application name used for Google Drive API authentication
- Should not be changed unless you have a custom Google Drive setup
- Example: `"ApplicationName": "My Custom ONI Mod"`

### Client Settings
These settings control the behavior when joining a multiplayer session.

#### `UseCustomMainMenu` (boolean, default: true)
- Whether to show the custom multiplayer overlay on the main menu
- `true`: Shows multiplayer buttons and options
- `false`: Uses vanilla main menu
- Example: `"UseCustomMainMenu": false`

#### `MaxMessagesPerPoll` (integer, default: 16)
- Maximum number of network messages to process per polling cycle for clients
- Generally lower than host setting to reduce client-side processing
- Range: 8-128
- Example: `"MaxMessagesPerPoll": 32`

#### `UseRandomPlayerColor` (boolean, default: true)
- Whether to use a random color for the player cursor
- `true`: Generates a random color each time
- `false`: Uses the color specified in `PlayerColor`
- Example: `"UseRandomPlayerColor": false`

#### `PlayerColor` Settings
RGB color values for the player cursor (only used if `UseRandomPlayerColor` is false).

##### `R`, `G`, `B` (integers, 0-255)
- Red, Green, and Blue color components
- Each value ranges from 0 (none) to 255 (full intensity)
- Examples:
  - Red: `"R": 255, "G": 0, "B": 0`
  - Blue: `"R": 0, "G": 100, "B": 255`
  - Purple: `"R": 128, "G": 0, "B": 128`

## Sample Configurations

### Default Configuration
```json
{
  "Host": {
    "MaxLobbySize": 4,
    "MaxMessagesPerPoll": 128,
    "SaveFileTransferChunkKB": 256,
    "CloudStorage": {
      "Provider": "SteamCloud",
      "GoogleDrive": {
        "ApplicationName": "ONI Multiplayer Mod"
      }
    }
  },
  "Client": {
    "UseCustomMainMenu": true,
    "MaxMessagesPerPoll": 16,
    "UseRandomPlayerColor": true,
    "PlayerColor": {
      "R": 255,
      "G": 255,
      "B": 255
    }
  }
}
```

### Google Drive Configuration
```json
{
  "Host": {
    "MaxLobbySize": 4,
    "MaxMessagesPerPoll": 128,
    "SaveFileTransferChunkKB": 256,
    "CloudStorage": {
      "Provider": "GoogleDrive",
      "GoogleDrive": {
        "ApplicationName": "ONI Multiplayer Mod"
      }
    }
  },
  "Client": {
    "UseCustomMainMenu": true,
    "MaxMessagesPerPoll": 16,
    "UseRandomPlayerColor": true,
    "PlayerColor": {
      "R": 255,
      "G": 255,
      "B": 255
    }
  }
}
```

### Performance-Optimized Configuration
```json
{
  "Host": {
    "MaxLobbySize": 6,
    "MaxMessagesPerPoll": 256,
    "SaveFileTransferChunkKB": 512,
    "CloudStorage": {
      "Provider": "SteamCloud",
      "GoogleDrive": {
        "ApplicationName": "ONI Multiplayer Mod"
      }
    }
  },
  "Client": {
    "UseCustomMainMenu": true,
    "MaxMessagesPerPoll": 32,
    "UseRandomPlayerColor": false,
    "PlayerColor": {
      "R": 100,
      "G": 150,
      "B": 255
    }
  }
}
```

### Low-Bandwidth Configuration
```json
{
  "Host": {
    "MaxLobbySize": 3,
    "MaxMessagesPerPoll": 64,
    "SaveFileTransferChunkKB": 128,
    "CloudStorage": {
      "Provider": "SteamCloud",
      "GoogleDrive": {
        "ApplicationName": "ONI Multiplayer Mod"
      }
    }
  },
  "Client": {
    "UseCustomMainMenu": true,
    "MaxMessagesPerPoll": 8,
    "UseRandomPlayerColor": true,
    "PlayerColor": {
      "R": 255,
      "G": 255,
      "B": 255
    }
  }
}
```

## Troubleshooting

### Configuration Not Loading
- Ensure the JSON syntax is valid (no trailing commas, proper quotes)
- Check that the file is in the correct directory
- Verify file permissions allow reading

### Performance Issues
- Reduce `MaxMessagesPerPoll` values
- Lower `MaxLobbySize`
- Decrease `SaveFileTransferChunkKB` for slow connections

### Connection Problems
- Increase `SaveFileTransferChunkKB` for faster transfers
- Adjust `MaxMessagesPerPoll` based on network conditions

## Notes
- The configuration file is automatically created with default values if it doesn't exist
- Changes to the configuration require restarting the game
- Invalid values will be replaced with defaults and logged as warnings
- **Steam Cloud is the recommended provider** for most users due to no setup requirements
- Google Drive setup is only needed if you require more than 100MB storage or have Steam Cloud disabled
