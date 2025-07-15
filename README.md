![Logo](https://i.imgur.com/GCIbhpn.png)
# ONI Together: An Oxygen Not Included Multiplayer Mod

> **Note:** This is a work-in-progress project in early alpha stage. Not to be confused with [onimp/oni_multiplayer](https://github.com/onimp/oni_multiplayer).

A multiplayer mod that introduces real-time collaborative gameplay to *Oxygen Not Included*, featuring a custom networking layer, Steam integration, and synchronized game mechanics.

[![Discord](https://img.shields.io/discord/DISCORD_ID?color=7289da&label=Discord&logo=discord&logoColor=white)](https://discord.gg/jpxveK6mmY)
[![License](https://img.shields.io/github/license/Lyraedan/Oxygen_Not_Included_Multiplayer)](LICENSE.md)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()

## Quick Start

1. **Download** the latest release from the releases page
2. **Install** following the [installation guide](docs/INSTALL.md)
3. **Configure** using the [configuration guide](docs/CONFIGURATION.md)
4. **Join the community** on [Discord](https://discord.gg/jpxveK6mmY)

## Documentation

- 📖 [Installation Guide](docs/INSTALL.md) - How to install and set up the mod
- ⚙️ [Configuration Guide](docs/CONFIGURATION.md) - Detailed configuration options
- 🔧 [Development Guide](docs/DEVELOPMENT.md) - For contributors and developers
- 📋 [Changelog](CHANGELOG.md) - Version history and changes

## Project Status

### 🎯 Current Version: 0.1.0-alpha

**Development Stage:** Early Alpha - Core functionality implemented, expect bugs and incomplete features.

### ✅ Stable Features
- Steam P2P networking and lobby system
- Real-time chat with themed UI
- Player cursor visualization
- Basic tool synchronization (Build, Cancel, Deconstruct, Priority)
- Configuration system with auto-generated defaults
- Debug tools and console (Shift+F1)

### 🚧 Work in Progress
- Advanced tool synchronization (Pipes, Wires, Mopping, Sweeping)
- Enhanced duplicant behavior synchronization
- Storage and inventory management
- Performance optimizations

### ⚠️ Known Limitations
- Gas, temperature, and fluid simulation remains client-side
- Some UI elements may not sync properly
- Large save files may transfer slowly
- Limited to small groups (2-4 players recommended)

---

## Demo

![Multiplayer Demo](https://i.imgur.com/VfiPUkn.jpeg)

---

## What's Done

- Network infrastructure for sending and receiving packets  
- Creating, joining, and leaving lobbies  
- Host, Client and Session detection  
- Steam overlay support  
  - Ability to join/invite friends
- Debug Tools
  - Bespoke console
  - Hierarchy viewer
  - Debug Menu (Open with Shift+F1)
    
  Debug Menu contains:
    - Open other menus
    - Test steamworks instantiation
    - Create Lobby
    - Leave Lobby

- Chat box
    - Fully synchronized
    - Includes Join / Leave messages of other users
    - Themed to look like other UI elements (95% finished)
    - Expandable
    - Can be dragged and moved around
 
 - Synchronization
    - Building tool
    - Cancel tool
    - Deconstruct tool
    - Priority tool
    - Sweep tool
    - Mop tool
    - Digging
    - World Cycle
      > (Controlled by the host)
    - Rough duplicant position and orientation synchronization
      > They at least move smoothly. They only snap if they fall out of sync
    - Rough Duplicant animation synchronization
      > Some animations don't play properly and get stuck on 1 frame like walking, running, climbing etc. Others seem fine
    - Move To tool synchronization
      > When you click a duplicant and use the "Move To Location" tool
    - "Trigger" synchronization
      > (its rough and not 100% but it lets duplicants pull out their dig tools, suck up tools etc)
    - Hard Sync
      > (At the start of every new cycle the server will perform a hard sync, which basically boots out all the clients and makes them redownload the map before automatically reconnecting them)
    - Player cursors
      > See where other players are pointing, the tool they are using and they are color coded.
    - Save file synchronization via Google Drive
      > Using Google Drive allows synchronization of worlds larger then 10MB. A limit the other multiplayer mod has. Without it, this mod would have the same 10MB limit. This however comes at the cost of a small but simple setup process by hosts that only needs to be completed once. The guide can be found [here](https://github.com/Lyraedan/Oxygen_Not_Included_Multiplayer/wiki/Google-Drive-Setup-Guide)

- Synchronized UI Elements
     - World Cycle

- Configuration file
   > Change lobby size, polling rates and player color (the color other players see you as)

---

## Work in Progress
- Tool synchronization (Building, Wire building, Pipe building, Mopping, Sweeping etc)
  > There is a rough sync attempt with wires but its unfinished any of the smaller tools (like sweep) are not synced (mopping, attacking, ranching etc)
- Storage sync
  > This right now relies on hard sync
- Gas, Temp, Fluid sync
  > I think this will stay to hard sync and let the client interpret how they should flow

## Known issues
- Theres alot of issues right now. But once these are ironed out a release will be put out on the steam workshop which will later be linked here

- Crash when connecting to a host that has alot of things going on.
  > Like duplicants actively digging etc (This happens sometimes)
- The loading screens disappear when connecting to a host / hard syncing etc
- Inviting from the pause menu does not invite the player
  >(Thanks steam x_x)
- Clients seem to get double the resource drops
  > Not sure why considering they get passed the hosts values
- Clients can sometimes trigger their own tasks which causes them to fall out of sync
  > Synchronization is properly regained when a hard sync occures
- Clients can have ceiling collapses when the host doesn't and vise versa
- When sweeping the clients can fall out of sync
  > Synchronization is properly regained when a hard sync occures
- When placing wires they don't look like they are connected properly for other players
  > When other players place them though they suddenly look connected to something and sometimes won't build?
- Sometimes after a hard sync clients can't seem to process incoming packets.
  > To fix this the client should just restart their game and rejoin

## Found an issue?
Raise it on the issues page. Please at least try to include a video if you can. It makes replicating it so much easier

---

## What's Planned

- World state synchronization
- Seamless mid-game hosting (start and stop multiplayer at any point during gameplay)  
- Menu and UI synchronization  
- Additional features to be announced
- More item synchronization (storage, research, skill points etc)

---

## Why not just contribute to the old multiplayer mod?

I like the old multiplayer mod — I do — and kudos to the guys that made it. But its implementation is very limited without a lot of extra effort, if not a full rewrite.  
On top of this, it hasn't seen activity in over 6 months.  
> **NOTE:** as of June 6th 2025, when I started this project.

Initially it was just conceptual, but once I got lobbies and packets set up, I knew I was onto something.

## Setup

To get started with building the mod, follow these steps:

1. **Install .NET Framework 4.7.2**  
   Make sure you have [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472) installed on your system.

2. **Clone the repository**  
   ```bash
   git clone https://github.com/Lyraedan/Oxygen_Not_Included_Multiplayer.git
   cd Oxygen_Not_Included_Multiplayer
   ```

3. **Build the project**  
   
   **Option A: Using the build script (Recommended)**
   ```powershell
   # Windows PowerShell
   .\build.ps1
   
   # Or with specific configuration
   .\build.ps1 -Configuration Release -Package
   ```
   
   **Option B: Using .NET CLI**
   ```bash
   dotnet build ONI_MP.sln
   ```
   
   **Option C: Using Visual Studio**
   Open `Oni_MP.sln` in Visual Studio and build the solution.

4. **Configure game path (if needed)**  
   The project is configured to auto-detect your ONI installation. If the build fails to find game assemblies, update the `ManagedPath` in `src/LUKES_ONI_MP/ONI_MP.csproj`:
   ```xml
   <ManagedPath>C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed</ManagedPath>
   ```

5. **Find your built mod**  
   After building, the mod files will be in:
   - Complete mod package: `BuildOutput/`
   - Built DLL: `BuildOutput/ONI_MP.dll`

### Project Structure

```
Oxygen_Not_Included_Multiplayer/
├── docs/                     # Documentation
├── src/                      # Source code
│   ├── LUKES_ONI_MP/        # Main project source
├── BuildOutput/             # Build output
├── build.ps1               # Build automation script
├── ONI_MP.sln             # Root solution file
└── README.md              # This file
```

---

## Contributing

Contributions are welcome!  
If you have improvements, fixes, or new features, feel free to open a Pull Request.

Please make sure your changes are clear and well-documented where necessary.

---

## License

This project is licensed under the MIT License.  
Copyright (c) 2023 Zuev Vladimir, Denis Pakhorukov

