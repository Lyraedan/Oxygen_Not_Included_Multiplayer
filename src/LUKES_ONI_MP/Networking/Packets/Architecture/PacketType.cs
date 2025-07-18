using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.Architecture
{
    public enum PacketType : byte
    {
        ChatMessage = 1,
        EntityPosition = 2,
        ChoreAssignment = 3,    // Immediate chore assignments for direct duplicant coordination
        WorldData = 4,          // Keeping for now, might find a use
        WorldDataRequest = 5,   // Keeping for now, might find a use
        WorldUpdate = 6,        // Environmental Systems: Real-time synchronization of gas flow, pressure, temperature, and fluid dynamics
        Instantiations = 7,     // Batched instantiations - Not in use atm
        NavigatorPath = 8,
        SaveFileRequest = 9,
        SaveFileChunk = 10,
        Diggable = 11,
        DigComplete = 12,
        PlayAnim = 13,
        Build = 14,
        BuildComplete = 15,
        WorldDamageSpawnResource = 16,
        WorldCycle = 17,
        Cancel = 18,
        Deconstruct = 19,
        DeconstructComplete = 20,
        WireBuild = 21,
        ToggleMinionEffect = 22,
        ToolEquip = 23,
        DuplicantCondition = 24,
        MoveToLocation = 25,     // Movement from the MoveTo tool
        Prioritize = 26,
        Clear = 27,               // Sweeping, Mopping etc
        ClientReadyStatus = 28,
        AllClientsReady = 29,
        ClientReadyStatusUpdate = 30,
        EventTriggered = 31,
        HardSync = 32,
        HardSyncComplete = 33, // Not in use atm
        Disinfect = 34,
        SpeedChange = 35,
        PlayerCursor = 36,
        GoogleDriveFileShare = 37,
        SteamP2PFileShare = 38,
        ResourceTransfer = 39,
        StorageUpdate = 40,
        InventorySync = 41,
        PickupableAction = 42,
        ResourceDrop = 43,
        AtmosphericChange = 44,  // Environmental Systems: Gas composition and atmospheric pressure changes
        FluidDynamics = 45,      // Environmental Systems: Pipe flows and liquid movement
        ResearchProgress = 46,   // Research & Skills: Research progress synchronization
        SkillPoints = 47,        // Research & Skills: Duplicant skill points and abilities
        TechnologyUnlock = 48,   // Research & Skills: Technology unlocks across clients
        
        // Duplicant Behavior synchronization (Types 49-54)
        WorkAssignment = 49,     // Work chore assignments and priorities
        IdleBehavior = 50,       // Idle states, downtime activities, recreation
        SleepBehavior = 51,      // Sleep cycles, bed assignments, rest patterns
        StressBehavior = 52,     // Stress responses, emotional outbursts, moods
        PathfindingUpdate = 53,  // Enhanced pathfinding coordination beyond basic NavigatorPath
        BehaviorState = 54,      // General duplicant behavior states (eating, working, moving, etc.)
        
        // Enhanced Animation Synchronization (Types 55-59)
        AnimationSync = 55,      // Comprehensive animation state synchronization
        AnimationFrame = 56,     // Specific animation frame updates
        AnimationLoop = 57,      // Animation loop control and timing
        AnimationTransition = 58, // Smooth animation transitions between states
        AnimationSpeed = 59,     // Dynamic animation speed adjustments
        
        // Building & Construction Systems (Types 60-69)
        PipeBuild = 60,          // Pipe construction and connections - NOT IMPLEMENTED
        PipeConnection = 61,     // Pipe connection states and flow - NOT IMPLEMENTED
        BuildingRotation = 62,   // Building orientation and rotation sync - NOT IMPLEMENTED
        MaterialSelection = 63,  // Construction material choices - NOT IMPLEMENTED
        BuildingUpgrade = 64,    // Building upgrades and modifications - NOT IMPLEMENTED
        BuildingConfig = 65,     // Building configuration and settings - NOT IMPLEMENTED
        MultiTileBuilding = 66,  // Complex multi-tile building coordination - NOT IMPLEMENTED
        BuildingDamage = 67,     // Building damage and repair states - NOT IMPLEMENTED
        BlueprintShare = 68,     // Shared building blueprints - NOT IMPLEMENTED
        BuildQueue = 69,         // Construction queue coordination - NOT IMPLEMENTED
        
        // Advanced World Systems (Types 70-79)
        PowerGrid = 70,          // Electrical power grid synchronization - NOT IMPLEMENTED
        AutomationSignal = 71,   // Automation signals and logic - NOT IMPLEMENTED
        ConveyorSystem = 72,     // Conveyor belt and rail systems - NOT IMPLEMENTED
        ElevatorSync = 73,       // Elevator and transport tube sync - NOT IMPLEMENTED
        TemperatureSync = 74,    // Real-time temperature propagation - NOT IMPLEMENTED
        GasFlowSync = 75,        // Gas movement and pressure sync - NOT IMPLEMENTED
        LiquidFlowSync = 76,     // Liquid flow in pipes and containers - NOT IMPLEMENTED
        DiseaseSpread = 77,      // Disease propagation and infection - NOT IMPLEMENTED
        WeatherSystem = 78,      // Weather events and meteor showers - NOT IMPLEMENTED
        WorldgenSync = 79,       // World generation and asteroid sync - NOT IMPLEMENTED
        
        // Duplicant Advanced Systems (Types 80-89)
        DuplicantNeeds = 80,     // Hunger, oxygen, bladder, sleep needs - NOT IMPLEMENTED
        DuplicantMoods = 81,     // Mood effects and emotional states - NOT IMPLEMENTED
        DuplicantTraits = 82,    // Personality traits and characteristics - NOT IMPLEMENTED
        DuplicantRelations = 83, // Social relationships between duplicants - NOT IMPLEMENTED
        DuplicantHealth = 84,    // Health status, injuries, and medical - NOT IMPLEMENTED
        DuplicantEquipment = 85, // Equipment and clothing synchronization - NOT IMPLEMENTED
        DuplicantSchedule = 86,  // Work schedules and shift assignments - NOT IMPLEMENTED
        DuplicantSkillUse = 87,  // Active skill usage and bonuses - NOT IMPLEMENTED
        DuplicantMemory = 88,    // Memory system and learned behaviors - NOT IMPLEMENTED
        DuplicantDeath = 89,     // Death, revival, and graveyard sync - NOT IMPLEMENTED
        
        // Resource & Economy Systems (Types 90-99)
        ResourceProduction = 90, // Resource generation rates and production - NOT IMPLEMENTED
        ResourceConsumption = 91, // Resource usage and consumption tracking - NOT IMPLEMENTED
        ResourceQuality = 92,    // Resource quality and temperature states - NOT IMPLEMENTED
        ResourceDiscovery = 93,  // New resource discoveries and geysers - NOT IMPLEMENTED
        ResourceReservation = 94, // Resource allocation and reservations - NOT IMPLEMENTED
        ResourceDecay = 95,      // Resource spoilage and decay timers - NOT IMPLEMENTED
        CraftingQueue = 96,      // Manufacturing and crafting queues - NOT IMPLEMENTED
        RecipeUnlock = 97,       // Recipe discoveries and cooking - NOT IMPLEMENTED
        TradeSystem = 98,        // Resource trading between players - NOT IMPLEMENTED
        EconomyBalance = 99,     // Economic balance and resource values - NOT IMPLEMENTED
        
        // UI & Interface Systems (Types 100-109)
        UIMenuSync = 100,        // Menu states and selections - NOT IMPLEMENTED
        UIScreenShare = 101,     // Shared screen viewing (research, etc.) - NOT IMPLEMENTED
        UINotification = 102,    // Alert and notification synchronization - NOT IMPLEMENTED
        UIPriorityPanel = 103,   // Priority panel settings sync - NOT IMPLEMENTED
        UIJobAssignment = 104,   // Job assignment interface sync - NOT IMPLEMENTED
        UIResearchScreen = 105,  // Research screen coordination - NOT IMPLEMENTED
        UIVitalsPanel = 106,     // Vitals and statistics panel sync - NOT IMPLEMENTED
        UIWorldMap = 107,        // World map and asteroid view sync - NOT IMPLEMENTED
        UIBuildMenu = 108,       // Building menu selections and filters - NOT IMPLEMENTED
        UITimeControl = 109,     // Time control panel synchronization - NOT IMPLEMENTED
        
        // Advanced Features (Types 110-119)
        PlayerPermissions = 110, // Player role and permission system - NOT IMPLEMENTED
        SessionControl = 111,    // Session hosting and management - NOT IMPLEMENTED
        ConflictResolution = 112, // Conflict resolution for simultaneous actions - NOT IMPLEMENTED
        StateVersioning = 113,   // World state versioning and tracking - NOT IMPLEMENTED
        PredictiveSync = 114,    // Predictive synchronization and smoothing - NOT IMPLEMENTED
        PerformanceMetrics = 115, // Network and performance monitoring - NOT IMPLEMENTED
        AntiCheat = 116,         // Anti-cheat and validation systems - NOT IMPLEMENTED
        SaveGameSync = 117,      // Enhanced save game synchronization - NOT IMPLEMENTED
        ModCompatibility = 118,  // Mod compatibility and synchronization - NOT IMPLEMENTED
        CrossPlatform = 119,     // Cross-platform compatibility layer - NOT IMPLEMENTED
        
        // Specialized Game Systems (Types 120-129)
        RocketSystem = 120,      // Rocket construction and space travel - NOT IMPLEMENTED
        SpaceExploration = 121,  // Space missions and asteroid exploration - NOT IMPLEMENTED
        AsteroidGeneration = 122, // Procedural asteroid generation sync - NOT IMPLEMENTED
        CreatureSystem = 123,    // Critter behavior and breeding - NOT IMPLEMENTED
        PlantGrowth = 124,       // Plant growth and farming cycles - NOT IMPLEMENTED
        FoodSystem = 125,        // Food preparation and quality - NOT IMPLEMENTED
        AtmosphereSystem = 126,  // Atmospheric composition detailed sync - NOT IMPLEMENTED
        RadiationSystem = 127,   // Radiation exposure and protection - NOT IMPLEMENTED
        MeteorShower = 128,      // Meteor impacts and damage - NOT IMPLEMENTED
        GeysersSystem = 129,     // Geyser behavior and output - NOT IMPLEMENTED
        
        // Future Expansion (Types 130-139)
        CustomContent = 130,     // Custom content sharing system - NOT IMPLEMENTED
        PlayerStats = 131,       // Player statistics and achievements - NOT IMPLEMENTED
        VoiceChat = 132,         // Integrated voice communication - NOT IMPLEMENTED
        ScreenShare = 133,       // Screen sharing and spectator mode - NOT IMPLEMENTED
        Replay = 134,            // Game replay and recording system - NOT IMPLEMENTED
        Tournaments = 135,       // Tournament and competitive modes - NOT IMPLEMENTED
        Challenges = 136,        // Shared challenges and objectives - NOT IMPLEMENTED
        Leaderboards = 137,      // Community leaderboards - NOT IMPLEMENTED
        WorldSharing = 138,      // World template sharing - NOT IMPLEMENTED
        CommunityFeatures = 139, // Community integration features - NOT IMPLEMENTED
        
        // Debug & Development (Types 140-149)
        DebugSync = 140,         // Debug information synchronization - NOT IMPLEMENTED
        PerformanceProfiler = 141, // Network performance profiling - NOT IMPLEMENTED
        StateInspector = 142,    // Real-time state inspection tools - NOT IMPLEMENTED
        NetworkDiagnostics = 143, // Network diagnostics and troubleshooting - NOT IMPLEMENTED
        ErrorReporting = 144,    // Automated error reporting system - NOT IMPLEMENTED
        MetricsCollection = 145, // Usage metrics and analytics - NOT IMPLEMENTED
        BetaFeatures = 146,      // Beta feature testing framework - NOT IMPLEMENTED
        DeveloperTools = 147,    // Developer debugging and testing tools - NOT IMPLEMENTED
        RemoteConsole = 148,     // Remote console and command execution - NOT IMPLEMENTED
        SystemMonitoring = 149   // System resource monitoring - NOT IMPLEMENTED
    }
}
