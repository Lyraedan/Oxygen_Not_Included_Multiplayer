# Changelog

All notable changes to the ONI Together multiplayer mod will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial multiplayer networking infrastructure
- Steam P2P lobby system with join/invite functionality
- Real-time chat system with themed UI
- Debug tools including console, hierarchy viewer, and debug menu (Shift+F1)
- Basic tool synchronization (Build, Cancel, Deconstruct, Priority)
- Configuration system with automatic default handling
- Google Drive integration for save file sharing
- Player cursor visualization across clients
- Network identity system for object synchronization

### Technical
- Harmony-based non-invasive game patching
- Packet-based network communication system
- Component architecture for modular functionality
- Robust error handling and logging system
- Automated build configuration with post-build actions

### Known Issues
- Chore assignment synchronization disabled (work in progress)
- Limited world state synchronization
- Save file transfer may be slow on poor connections
- Some UI elements may not sync properly

## [0.1.0-alpha] - 2025-01-15

### Added
- Initial alpha release
- Core multiplayer infrastructure
- Basic lobby functionality
- Essential debugging tools

### Project Structure
- Renamed project folder from ClassLibrary1 to LUKES_ONI_MP
- Organized documentation into dedicated docs/ folder
- Added standardized project configuration files
- Improved build system with automatic file copying

### Configuration
- Created comprehensive configuration system
- Added sample configuration files
- Implemented runtime configuration validation
- Added detailed configuration documentation

### Development
- Added .editorconfig for consistent code formatting
- Created Directory.Build.props for global build settings
- Organized project structure with clear separation of concerns
- Added development documentation and guidelines

---

## Development Notes

### Version Numbering
- **Major.Minor.Patch-Prerelease**
- Major: Breaking changes or significant feature additions
- Minor: New features, backward compatible
- Patch: Bug fixes and small improvements
- Prerelease: alpha, beta, rc1, etc.

### Release Process
1. Update version in Directory.Build.props
2. Update CHANGELOG.md with new version
3. Test all major functionality
4. Create release build
5. Package mod files
6. Tag release in git
7. Publish to appropriate channels
