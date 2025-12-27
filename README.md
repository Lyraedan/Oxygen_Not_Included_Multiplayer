![Logo](https://i.imgur.com/GCIbhpn.png)

# ONI Together - Direct Connection Fork

This fork adds Direct IP Connection support to the ONI Together multiplayer mod.

## Changes from Original

### New Features
- Direct TCP connection (bypass Steam P2P)
- LAN play support without Steam relay
- Lower latency for local network play

### Modified Files
- ClassLibrary1/Networking/DirectConnection.cs (NEW)
- ClassLibrary1/Networking/PacketSender.cs (MODIFIED)
- ClassLibrary1/DebugTools/DebugMenu.cs (MODIFIED)

---

## Build Instructions

### Requirements
- Visual Studio 2022 (or later)
- .NET Framework 4.7.2
- Oxygen Not Included (Steam version)

### Setup Steps

1. Copy Directory.Build.props.default to Directory.Build.props.user

2. Edit Directory.Build.props.user with your paths:
   - GameLibsFolder: Your ONI installation Managed folder
   - ModFolder: Your mods dev folder

3. Open Oni_MP.sln in Visual Studio

4. Build with Ctrl + Shift + B

5. If first build fails, close VS and reopen, then build again

---

## How to Use

### In-Game Hotkeys
- Shift + F1: Open Debug Menu
- Shift + F2: Open Direct Connect Window

### Host a Game
1. Press Shift + F2
2. Click Create Room (Host)
3. Note your IP address (e.g., 192.168.1.100:11000)
4. Share IP with friend

### Join a Game
1. Press Shift + F2
2. Enter host IP address
3. Enter port (default: 11000)
4. Click Join Room (Join)

---

## Network Setup

### LAN Play
- Both players on same network
- Use local IP (192.168.x.x or 10.x.x.x)
- No additional setup needed

### Internet Play
- Option A: Port forward 11000 TCP on router
- Option B: Use ZeroTier (https://www.zerotier.com/)
- Option C: Use Tailscale (https://tailscale.com/)

---

## Troubleshooting

### Build Errors
- Check paths in Directory.Build.props.user
- Run game once first to create Klei folders
- Close VS and reopen if first build fails

### Connection Issues
- Check firewall allows port 11000
- Verify IP address is correct
- Ensure both have same mod version

---

## License
MIT License - Same as original project

## Credits
- Original mod: Lyraedan
- Direct Connection: Added for LAN/IP play



# ONI Together: An Oxygen Not Included Multiplayer Mod (WIP)

> **Note:** This is a work-in-progress project. Not to be confused with [onimp/oni_multiplayer](https://github.com/onimp/oni_multiplayer).

A new mod that introduces multiplayer functionality to _Oxygen Not Included_, featuring a custom networking layer and lobby system.

> **Note:** This mod is in the very early pre-pre alpha stage.

Join the [Discord](https://discord.gg/jpxveK6mmY)

Steam workshop: [You can find its workshop page here](https://steamcommunity.com/sharedfiles/filedetails/?id=3630759126)

---

## Demo

![Multiplayer Demo](https://i.imgur.com/VfiPUkn.jpeg)

---

## Whats done and in progress?

A public trello board exists that tracks whats been done, and what is coming in the next update you can find that [here](https://trello.com/b/kq7yVWyU/oxygen-not-included-together)

## Found an issue?

Raise it on the issues page, and make a bug report on the discord.

Please at least try to include a video if you can. It makes replicating it so much easier

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

   ```
   git clone https://github.com/Lyraedan/Oxygen_Not_Included_Multiplayer.git
   ```

3. **Open the solution**  
   Open the `.sln` file in Visual Studio (or your preferred C# IDE).

4. **Copy the file `Directory.Build.props.default` and rename it to `Directory.Build.props.user` file, then adjust the paths**  
   Open the `.csproj` file and find the file under `Solution Items`.
   Copy it and rename the copy to `Directory.Build.props.user`, then open that new file.
   Change its variable `GameLibsFolder` to point to your local `OxygenNotIncluded_Data/Managed` folder, for example:
   ```xml
   <GameLibsFolder>C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed</GameLibsFolder>
   ```
   then adjust the variable `ModFolder` to point at your local dev folder, for example:
   ```xml
   <ModFolder>E:\Documents\Klei\OxygenNotIncluded\mods\dev</ModFolder>
   ```
5. **(optional) Restore NuGet packages**

6. **Build the project**  
   Once the `ManagedPath` is correctly set, build the project.  
   If everything is configured correctly, the build should succeed.
   If there are missing reference errors, restart Visual Studio, the solution creates a publicized reference library the first time a build runs and this can confuse the IDE.

---

## Contributing

Contributions are welcome!  
If you have improvements, fixes, or new features, feel free to open a Pull Request.

Please make sure your changes are clear and well-documented where necessary.

---

## License

This project is licensed under the MIT License.  
Copyright (c) 2023 Zuev Vladimir, Denis Pakhorukov
