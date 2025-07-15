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
        ChoreAssignment = 3,    // Not in use atm
        WorldData = 4,          // Keeping for now, might find a use
        WorldDataRequest = 5,   // Keeping for now, might find a use
        WorldUpdate = 6,        // Batched world updates -  Not in use atm
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
        GoogleDriveFileShare = 37
    }
}
