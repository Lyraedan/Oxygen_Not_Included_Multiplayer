# Development Guide

This document provides information for developers working on the ONI Together multiplayer mod.

## Project Structure

```
Oxygen_Not_Included_Multiplayer/
├── .editorconfig              # Code formatting configuration
├── .gitignore                 # Git ignore patterns
├── Directory.Build.props      # Global MSBuild properties
├── Oni_MP.sln                 # Visual Studio solution file
├── README.md                  # Project overview and features
├── LICENSE.md                 # Project license
├── compile.sh                 # Build script for Linux/macOS
│
├── docs/                      # Documentation files
│   ├── CONFIGURATION.md       # Configuration guide
│   ├── INSTALL.md             # Installation instructions
│   └── DEVELOPMENT.md         # This file
│
└── src/                       # Source code directory
    └── LUKES_ONI_MP/          # Main source code
        ├── ONI_MP.csproj      # Project file
        ├── Configuration.cs   # Configuration management
        ├── MultiplayerMod.cs  # Main mod entry point
        ├── mod.yaml           # Mod metadata file
        ├── mod_info.yaml      # Additional mod info
        ├── multiplayer_settings.json # Default configuration
        │
        ├── Assets/            # Embedded resources
        │   ├── *.png          # UI images
        │   └── bundles/       # Asset bundles
        │
        ├── Cloud/             # Cloud storage integration
        ├── DebugTools/        # Development and debugging tools
        ├── Menus/             # UI and menu systems
        ├── Misc/              # Utility classes
        ├── Networking/        # Core networking functionality
        │   ├── Components/    # Network components
        │   ├── Packets/       # Network packet definitions
        │   └── States/        # Network state management
        └── Patches/           # Harmony patches
            ├── ChoresPatches/ # Chore system patches
            ├── GamePatches/   # Core game patches
            ├── KleiPatches/   # Klei-specific patches
            ├── LoadingOverlayPatches/
            ├── MainMenuScreenPatches/
            ├── NavigationPatches/
            ├── ToolPatches/
            └── World/         # World-related patches
```

## Development Setup

### Prerequisites

- .NET Framework 4.7.2 SDK
- Visual Studio 2019/2022 or VS Code with C# extension
- Oxygen Not Included (Steam version)

### Building the Project

#### Windows (Visual Studio)
1. Open `Oni_MP.sln` in Visual Studio 
2. Build the solution (Ctrl+Shift+B)
3. The mod will be automatically copied to `BuildOutput/`

#### Windows (Command Line)
```powershell
# Using the build script (recommended)
.\build.ps1

# Or using .NET CLI directly
dotnet build ONI_MP.sln
```

#### Linux/macOS
```bash
./compile.sh
```

#### Build Output

After building, you'll find the complete mod package in:
```
BuildOutput/
├── mod.yaml               # Mod metadata
├── mod_info.yaml          # Additional mod info
├── ONI_MP.dll             # Compiled mod assembly
├── multiplayer_settings.json # Default configuration
├── Google.Apis.*.dll      # Google Drive dependencies
├── Newtonsoft.Json.dll    # JSON serialization
├── CONFIGURATION.md       # Configuration documentation
└── INSTALL.md             # Installation guide
```

### Configuration

The project uses several configuration mechanisms:

1. **Directory.Build.props** - Global MSBuild properties
2. **.editorconfig** - Code formatting rules
3. **multiplayer_settings.json** - Runtime mod configuration

### Coding Standards

- Use 4 spaces for indentation
- Follow C# naming conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and small
- Use proper error handling and logging

### Architecture Overview

#### Core Components

1. **MultiplayerMod** - Main mod entry point and lifecycle management
2. **Configuration** - Settings management with automatic defaults
3. **GameServer/GameClient** - Network communication layer
4. **MultiplayerSession** - Session state management
5. **NetworkIdentity** - Object synchronization system

#### Key Patterns

- **Harmony Patches** - Non-invasive game modification
- **Packet System** - Structured network communication
- **Component Architecture** - Modular functionality
- **Singleton Pattern** - Global state management

#### Networking

The mod uses Steam P2P networking for:
- Session discovery and joining
- Real-time game state synchronization
- File transfer (save games)
- Chat and messaging

### Debugging

#### Debug Console
- Press Shift+F1 to open the debug menu
- View hierarchy and inspect game objects
- Monitor network traffic
- Test lobby creation and joining

#### Logging
- Use `DebugConsole.Log()` for general information
- Use `DebugConsole.LogWarning()` for warnings
- Use `DebugConsole.LogError()` for errors
- Logs are visible in the debug console and game logs

### Testing

#### Manual Testing
1. Start the game with the mod installed
2. Create a lobby from the main menu
3. Have another player join the lobby
4. Test basic gameplay synchronization

#### Common Test Scenarios
- Building and deconstructing objects
- Duplicant task assignment
- Resource management
- Save/load functionality
- Chat system

### Contributing

1. Follow the existing code style and patterns
2. Test your changes thoroughly
3. Update documentation as needed
4. Use meaningful commit messages
5. Keep pull requests focused and small

### Common Issues

#### Build Errors
- Ensure game path is correct in project settings
- Verify all required assemblies are referenced
- Check .NET Framework version compatibility

#### Runtime Issues
- Check mod loading order
- Verify Harmony patches are applying correctly
- Monitor debug console for errors
- Ensure Steam is running and logged in

### Performance Considerations

- Network packet frequency and size
- Harmony patch overhead
- Memory usage for large multiplayer sessions
- UI update frequency

### Future Improvements

- Automated testing framework
- Performance profiling tools
- Better error recovery mechanisms
- Enhanced synchronization systems
- Additional debugging utilities
