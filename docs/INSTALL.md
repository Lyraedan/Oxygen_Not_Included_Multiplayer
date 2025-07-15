# ONI Together - Installation Guide

This guide will help you install and set up the ONI Together multiplayer mod for Oxygen Not Included.

## Prerequisites

- Oxygen Not Included (Steam version)
- Steam client running and logged in
- Windows, macOS, or Linux

## Installation Steps

### Method 1: Manual Installation (Current)

1. **Download the mod files**
   - Get the latest `oni_mp` folder from the releases
   - Ensure it contains all required files:
     - `ONI_MP.dll`
     - `mod.yaml`
     - `mod_info.yaml`
     - `multiplayer_settings.json`
     - Configuration documentation files

2. **Locate your mods directory**
   ```
   Windows: %USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\
   macOS: ~/Documents/Klei/OxygenNotIncluded/mods/Local/
   Linux: ~/.config/unity3d/Klei/Oxygen Not Included/mods/Local/
   ```

3. **Install the mod**
   - Copy the entire `oni_mp` folder to your `mods/Local/` directory
   - If updating, replace the existing folder completely
   - The final path should be: `mods/Local/oni_mp/`

4. **Enable the mod**
   - Launch Oxygen Not Included
   - Go to the Mods menu
   - Find "ONI Together - Multiplayer Mod" in the list
   - Enable the mod
   - Restart the game

### Method 2: Steam Workshop (Coming Soon)

The mod will be available on the Steam Workshop in the future for easier installation and updates.

## Configuration

### Default Settings
The mod comes with default settings that work for most users. The configuration file `multiplayer_settings.json` will be automatically created with sensible defaults.

### Custom Configuration
To customize settings:

1. Navigate to the mod directory: `mods/Local/oni_mp/`
2. Edit `multiplayer_settings.json` with a text editor
3. Refer to `CONFIGURATION.md` for detailed setting explanations
4. Use `multiplayer_settings_sample.json` as a reference for alternative configurations

### Important Settings
- **MaxLobbySize**: Maximum players in a lobby (default: 4)
- **UseCustomMainMenu**: Enable multiplayer UI elements (default: true)
- **UseRandomPlayerColor**: Random player cursor colors (default: true)

## First-Time Setup

1. **Create a lobby**
   - From the main menu, look for multiplayer options
   - Click "Create Lobby" to host a session
   - Invite friends through Steam overlay

2. **Join a lobby**
   - Accept Steam invites from friends
   - Or use the join options in the main menu

3. **Test basic functionality**
   - Try building something simple
   - Test the chat system
   - Verify that actions are synchronized

## Troubleshooting

### Common Issues

#### Mod doesn't appear in mod list
- Verify the mod folder is in the correct location
- Check that all required files are present
- Ensure the folder is named exactly `oni_mp`

#### Game crashes on startup
- Check if you have conflicting mods
- Verify your game version is compatible
- Look for error messages in the game logs

#### Cannot create or join lobbies
- Ensure Steam is running and you're logged in
- Check your internet connection
- Verify Steam P2P networking isn't blocked by firewall

#### Synchronization issues
- Check the debug console (Shift+F1) for errors
- Verify all players have the same mod version
- Test with a smaller lobby size

### Debug Tools

Press **Shift+F1** in-game to open the debug menu, which provides:
- Debug console for viewing logs and errors
- Hierarchy viewer for inspecting game objects
- Network testing tools
- Lobby management options

### Getting Help

1. **Check the documentation**
   - Read `CONFIGURATION.md` for settings help
   - Review `DEVELOPMENT.md` for technical details

2. **Common solutions**
   - Restart the game after making configuration changes
   - Verify Steam is running and online
   - Check that all players have the same mod version

3. **Reporting issues**
   - Include your configuration file
   - Describe the steps to reproduce the problem
   - Provide any error messages from the debug console

## Uninstallation

To remove the mod:

1. Disable the mod in the game's mod menu
2. Delete the `oni_mp` folder from your mods directory
3. Restart the game

## Performance Tips

### For better performance:
- Reduce `MaxLobbySize` for slower computers
- Lower `MaxMessagesPerPoll` values in configuration
- Use smaller `SaveFileTransferChunkKB` for slow internet

### For better stability:
- Keep lobby sizes small (2-4 players recommended)
- Ensure all players have stable internet connections
- Save frequently when hosting

## Updates

### Updating the mod:
1. Download the latest version
2. Replace the entire `oni_mp` folder
3. Check if your configuration needs updates
4. Restart the game

The mod will preserve your configuration settings between updates when possible.

---

**Note**: This mod is in early development (pre-alpha stage). Expect bugs and incomplete features. Please report issues and provide feedback to help improve the mod.