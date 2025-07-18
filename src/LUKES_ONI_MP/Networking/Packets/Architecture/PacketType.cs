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
        DuplicantStress = 55,    // Duplicant stress level synchronization
        DuplicantStamina = 56,   // Duplicant stamina level synchronization
        DuplicantStressReaction = 57, // Duplicant stress reaction events
        DuplicantStressBehavior = 58, // Duplicant stress-related behaviors
        BuildingDamageFromStress = 59, // Building damage caused by stressed duplicants
        
        // Enhanced Animation Synchronization (Types 60-64)
        AnimationSync = 60,      // Comprehensive animation state synchronization
        AnimationFrame = 61,     // Specific animation frame updates
        AnimationLoop = 62,      // Animation loop control and timing
        AnimationTransition = 63, // Smooth animation transitions between states
        AnimationSpeed = 64,     // Dynamic animation speed adjustments
        
        // Building & Construction Systems (Types 65-74)
        PipeBuild = 65,          // Pipe construction and connections - NOT IMPLEMENTED
        PipeConnection = 66,     // Pipe connection states and flow - NOT IMPLEMENTED
        BuildingRotation = 67,   // Building orientation and rotation sync - NOT IMPLEMENTED
        MaterialSelection = 68,  // Construction material choices - NOT IMPLEMENTED
        BuildingUpgrade = 69,    // Building upgrades and modifications - NOT IMPLEMENTED
        BuildingConfig = 70,     // Building configuration and settings - NOT IMPLEMENTED
        MultiTileBuilding = 71,  // Complex multi-tile building coordination - NOT IMPLEMENTED
        BuildingDamage = 72,     // Building damage and repair states - NOT IMPLEMENTED
        BlueprintShare = 73,     // Shared building blueprints - NOT IMPLEMENTED
        BuildQueue = 74,         // Construction queue coordination - NOT IMPLEMENTED
        
        // Advanced World Systems (Types 75-84)
        PowerGrid = 75,          // Electrical power grid synchronization - NOT IMPLEMENTED
        AutomationSignal = 76,   // Automation signals and logic - NOT IMPLEMENTED
        ConveyorSystem = 77,     // Conveyor belt and rail systems - NOT IMPLEMENTED
        ElevatorSync = 78,       // Elevator and transport tube sync - NOT IMPLEMENTED
        TemperatureSync = 79,    // Real-time temperature propagation - NOT IMPLEMENTED
        GasFlowSync = 80,        // Gas movement and pressure sync - NOT IMPLEMENTED
        LiquidFlowSync = 81,     // Liquid flow in pipes and containers - NOT IMPLEMENTED
        DiseaseSpread = 82,      // Disease propagation and infection - NOT IMPLEMENTED
        WeatherSystem = 83,      // Weather events and meteor showers - NOT IMPLEMENTED
        WorldgenSync = 84,       // World generation and asteroid sync - NOT IMPLEMENTED
        
        // Duplicant Advanced Systems (Types 85-94)
        DuplicantNeeds = 85,     // Hunger, oxygen, bladder, sleep needs - NOT IMPLEMENTED
        DuplicantMoods = 86,     // Mood effects and emotional states - NOT IMPLEMENTED
        DuplicantTraits = 87,    // Personality traits and characteristics - NOT IMPLEMENTED
        DuplicantRelations = 88, // Social relationships between duplicants - NOT IMPLEMENTED
        DuplicantHealth = 89,    // Health status, injuries, and medical - NOT IMPLEMENTED
        DuplicantEquipment = 90, // Equipment and clothing synchronization - NOT IMPLEMENTED
        DuplicantSchedule = 91,  // Work schedules and shift assignments - NOT IMPLEMENTED
        DuplicantSkillUse = 92,  // Active skill usage and bonuses - NOT IMPLEMENTED
        DuplicantMemory = 93,    // Memory system and learned behaviors - NOT IMPLEMENTED
        DuplicantDeath = 94,     // Death, revival, and graveyard sync - NOT IMPLEMENTED
        
        // Resource & Economy Systems (Types 95-104)
        ResourceProduction = 95, // Resource generation rates and production - NOT IMPLEMENTED
        ResourceConsumption = 96, // Resource usage and consumption tracking - NOT IMPLEMENTED
        ResourceQuality = 97,    // Resource quality and temperature states - NOT IMPLEMENTED
        ResourceDiscovery = 98,  // New resource discoveries and geysers - NOT IMPLEMENTED
        ResourceReservation = 99, // Resource allocation and reservations - NOT IMPLEMENTED
        ResourceDecay = 100,      // Resource spoilage and decay timers - NOT IMPLEMENTED
        CraftingQueue = 101,      // Manufacturing and crafting queues - NOT IMPLEMENTED
        RecipeUnlock = 102,       // Recipe discoveries and cooking - NOT IMPLEMENTED
        TradeSystem = 103,        // Resource trading between players - NOT IMPLEMENTED
        EconomyBalance = 104,     // Economic balance and resource values - NOT IMPLEMENTED
        
        // UI & Interface Systems (Types 105-114)
        UIMenuSync = 105,        // Menu states and selections - NOT IMPLEMENTED
        UIScreenShare = 106,     // Shared screen viewing (research, etc.) - NOT IMPLEMENTED
        UINotification = 107,    // Alert and notification synchronization - NOT IMPLEMENTED
        UIPriorityPanel = 108,   // Priority panel settings sync - NOT IMPLEMENTED
        UIJobAssignment = 109,   // Job assignment interface sync - NOT IMPLEMENTED
        UIResearchScreen = 110,  // Research screen coordination - NOT IMPLEMENTED
        UIVitalsPanel = 111,     // Vitals and statistics panel sync - NOT IMPLEMENTED
        UIWorldMap = 112,        // World map and asteroid view sync - NOT IMPLEMENTED
        UIBuildMenu = 113,       // Building menu selections and filters - NOT IMPLEMENTED
        UITimeControl = 114,     // Time control panel synchronization - NOT IMPLEMENTED
        
        // Advanced Features (Types 115-124)
        PlayerPermissions = 115, // Player role and permission system - NOT IMPLEMENTED
        SessionControl = 116,    // Session hosting and management - NOT IMPLEMENTED
        ConflictResolution = 117, // Conflict resolution for simultaneous actions - NOT IMPLEMENTED
        StateVersioning = 118,   // World state versioning and tracking - NOT IMPLEMENTED
        PredictiveSync = 119,    // Predictive synchronization and smoothing - NOT IMPLEMENTED
        PerformanceMetrics = 120, // Network and performance monitoring - NOT IMPLEMENTED
        AntiCheat = 121,         // Anti-cheat and validation systems - NOT IMPLEMENTED
        SaveGameSync = 122,      // Enhanced save game synchronization - NOT IMPLEMENTED
        ModCompatibility = 123,  // Mod compatibility and synchronization - NOT IMPLEMENTED
        CrossPlatform = 124,     // Cross-platform compatibility layer - NOT IMPLEMENTED
        
        // Specialized Game Systems (Types 125-134)
        RocketSystem = 125,      // Rocket construction and space travel - NOT IMPLEMENTED
        SpaceExploration = 126,  // Space missions and asteroid exploration - NOT IMPLEMENTED
        AsteroidGeneration = 127, // Procedural asteroid generation sync - NOT IMPLEMENTED
        CreatureSystem = 128,    // Critter behavior and breeding - NOT IMPLEMENTED
        PlantGrowth = 129,       // Plant growth and farming cycles - NOT IMPLEMENTED
        FoodSystem = 130,        // Food preparation and quality - NOT IMPLEMENTED
        AtmosphereSystem = 131,  // Atmospheric composition detailed sync - NOT IMPLEMENTED
        RadiationSystem = 132,   // Radiation exposure and protection - NOT IMPLEMENTED
        MeteorShower = 133,      // Meteor impacts and damage - NOT IMPLEMENTED
        GeysersSystem = 134,     // Geyser behavior and output - NOT IMPLEMENTED
        
        // Future Expansion (Types 135-144)
        CustomContent = 135,     // Custom content sharing system - NOT IMPLEMENTED
        PlayerStats = 136,       // Player statistics and achievements - NOT IMPLEMENTED
        VoiceChat = 137,         // Integrated voice communication - NOT IMPLEMENTED
        ScreenShare = 138,       // Screen sharing and spectator mode - NOT IMPLEMENTED
        Replay = 139,            // Game replay and recording system - NOT IMPLEMENTED
        Tournaments = 140,       // Tournament and competitive modes - NOT IMPLEMENTED
        Challenges = 141,        // Shared challenges and objectives - NOT IMPLEMENTED
        Leaderboards = 142,      // Community leaderboards - NOT IMPLEMENTED
        WorldSharing = 143,      // World template sharing - NOT IMPLEMENTED
        CommunityFeatures = 144, // Community integration features - NOT IMPLEMENTED
        
        // Debug & Development (Types 145-154)
        DebugSync = 145,         // Debug information synchronization - NOT IMPLEMENTED
        PerformanceProfiler = 146, // Network performance profiling - NOT IMPLEMENTED
        StateInspector = 147,    // Real-time state inspection tools - NOT IMPLEMENTED
        NetworkDiagnostics = 148, // Network diagnostics and troubleshooting - NOT IMPLEMENTED
        ErrorReporting = 149,    // Automated error reporting system - NOT IMPLEMENTED
        MetricsCollection = 150, // Usage metrics and analytics - NOT IMPLEMENTED
        BetaFeatures = 151,      // Beta feature testing framework - NOT IMPLEMENTED
        DeveloperTools = 152,    // Developer debugging and testing tools - NOT IMPLEMENTED
        RemoteConsole = 153,     // Remote console and command execution - NOT IMPLEMENTED
        SystemMonitoring = 154,  // System resource monitoring - NOT IMPLEMENTED
        
        // Steam P2P File Transfer (Types 155-159)
        P2PFileRequest = 155,    // Request a file from peers
        P2PFileChunk = 156,      // File chunk data transfer
        P2PFileManifest = 157,   // File availability announcement
        P2PChunkRequest = 158,   // Request specific chunks
        P2PTransferComplete = 159 // Transfer completion notification
    }
}
