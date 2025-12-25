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
