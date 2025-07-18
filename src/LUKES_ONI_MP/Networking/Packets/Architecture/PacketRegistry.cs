using System;
using System.Collections.Generic;
using ONI_MP.Networking.Packets.SharedStorage;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.Packets.DuplicantActions;
using ONI_MP.Networking.Packets.Events;
using ONI_MP.Networking.Packets.Social;
using ONI_MP.Networking.Packets.Tools.Build;
using ONI_MP.Networking.Packets.Tools.Cancel;
using ONI_MP.Networking.Packets.Tools.Clear;
using ONI_MP.Networking.Packets.Tools.Deconstruct;
using ONI_MP.Networking.Packets.Tools.Dig;
using ONI_MP.Networking.Packets.Tools.Disinfect;
using ONI_MP.Networking.Packets.Tools.Move;
using ONI_MP.Networking.Packets.Tools.Prioritize;
using ONI_MP.Networking.Packets.Tools.Wire;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Packets.Resources;
using ONI_MP.Networking.Packets.Research;
using ONI_MP.Networking.Packets.DuplicantBehavior;

namespace ONI_MP.Networking.Packets.Architecture
{
    public static class PacketRegistry
    {
        private static readonly Dictionary<PacketType, Func<IPacket>> _constructors = new Dictionary<PacketType, Func<IPacket>>();

        public static void Register(PacketType type, Func<IPacket> constructor)
        {
            _constructors[type] = constructor;
        }

        public static IPacket Create(PacketType type)
        {
            return _constructors.TryGetValue(type, out var ctor)
                ? ctor()
                : throw new InvalidOperationException($"No packet registered for type {type}");
        }

        public static void RegisterDefaults()
        {
            Register(PacketType.ChoreAssignment, () => new ChoreAssignmentPacket());
            Register(PacketType.EntityPosition, () => new EntityPositionPacket());
            Register(PacketType.ChatMessage, () => new ChatMessagePacket());
            Register(PacketType.WorldData, () => new WorldDataPacket());
            Register(PacketType.WorldDataRequest, () => new WorldDataRequestPacket());
            Register(PacketType.WorldUpdate, () => new WorldUpdatePacket());
            Register(PacketType.NavigatorPath, () => new NavigatorPathPacket());
            Register(PacketType.SaveFileRequest, () => new SaveFileRequestPacket());
            Register(PacketType.SaveFileChunk, () => new SaveFileChunkPacket());
            Register(PacketType.Diggable, () => new DiggablePacket());
            Register(PacketType.DigComplete, () => new DigCompletePacket());
            Register(PacketType.PlayAnim, () => new PlayAnimPacket());
            Register(PacketType.Build, () => new BuildPacket());
            Register(PacketType.BuildComplete, () => new BuildCompletePacket());
            Register(PacketType.WorldDamageSpawnResource, () => new WorldDamageSpawnResourcePacket());
            Register(PacketType.WorldCycle, () => new WorldCyclePacket());
            Register(PacketType.Cancel, () => new CancelPacket());
            Register(PacketType.Deconstruct, () => new DeconstructPacket());
            Register(PacketType.DeconstructComplete, () => new DeconstructCompletePacket());
            Register(PacketType.WireBuild, () => new WireBuildPacket());
            Register(PacketType.ToggleMinionEffect, () => new ToggleMinionEffectPacket());
            Register(PacketType.ToolEquip, () => new ToolEquipPacket());
            Register(PacketType.DuplicantCondition, () => new DuplicantConditionPacket());
            Register(PacketType.MoveToLocation, () => new MoveToLocationPacket());
            Register(PacketType.Prioritize, () => new PrioritizePacket());
            Register(PacketType.Clear, () => new ClearPacket());
            Register(PacketType.ClientReadyStatus, () => new ClientReadyStatusPacket());
            Register(PacketType.ClientReadyStatusUpdate, () => new ClientReadyStatusUpdatePacket());
            Register(PacketType.AllClientsReady, () => new AllClientsReadyPacket());
            Register(PacketType.EventTriggered, () => new EventTriggeredPacket());
            Register(PacketType.HardSync, () => new HardSyncPacket());
            Register(PacketType.HardSyncComplete, () => new HardSyncCompletePacket());
            Register(PacketType.Disinfect, () => new DisinfectPacket());
            Register(PacketType.SpeedChange, () => new SpeedChangePacket());
            Register(PacketType.PlayerCursor, () => new PlayerCursorPacket());
            Register(PacketType.GoogleDriveFileShare, () => new GoogleDriveFileSharePacket());
            Register(PacketType.SteamP2PFileShare, () => new SteamP2PFileSharePacket());
            
            // Resource & Inventory Synchronization Packets
            Register(PacketType.ResourceTransfer, () => new ResourceTransferPacket());
            Register(PacketType.StorageUpdate, () => new StorageUpdatePacket());
            Register(PacketType.InventorySync, () => new InventorySyncPacket());
            Register(PacketType.PickupableAction, () => new PickupableActionPacket());
            Register(PacketType.ResourceDrop, () => new ResourceDropPacket());
            
            // Environmental Systems Packets
            Register(PacketType.AtmosphericChange, () => new AtmosphericChangePacket());
            Register(PacketType.FluidDynamics, () => new FluidDynamicsPacket());
            
            // Research & Skills Packets
            Register(PacketType.ResearchProgress, () => new ResearchProgressPacket());
            Register(PacketType.SkillPoints, () => new SkillPointsPacket());
            Register(PacketType.TechnologyUnlock, () => new TechnologyUnlockPacket());
            
            // Duplicant Behavior Packets
            Register(PacketType.WorkAssignment, () => new WorkAssignmentPacket());
            Register(PacketType.IdleBehavior, () => new IdleBehaviorPacket());
            Register(PacketType.SleepBehavior, () => new SleepBehaviorPacket());
            Register(PacketType.StressBehavior, () => new StressBehaviorPacket());
            Register(PacketType.PathfindingUpdate, () => new PathfindingUpdatePacket());
            Register(PacketType.BehaviorState, () => new BehaviorStatePacket());
            
            // Steam P2P File Transfer Packets
            Register(PacketType.P2PFileRequest, () => new P2PFileRequestPacket());
            Register(PacketType.P2PFileChunk, () => new P2PFileChunkPacket());
            Register(PacketType.P2PFileManifest, () => new P2PFileManifestPacket());
            Register(PacketType.P2PChunkRequest, () => new P2PChunkRequestPacket());
            Register(PacketType.P2PTransferComplete, () => new P2PTransferCompletePacket());
        }
    }
}
