using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
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
using ONI_MP.Networking.Packets.World;
using System;
using System.Collections.Generic;

namespace ONI_MP.Networking.Packets.Architecture
{
	public static class PacketRegistry
	{
		private static readonly Dictionary<int, Func<IPacket>> _constructors = new Dictionary<int, Func<IPacket>>();
        private static readonly Dictionary<Type, int> _typeToId = new Dictionary<Type, int>();
        private static int nextId = 0;

        public static bool HasRegisteredPacket(int type)
        {
            return _constructors.ContainsKey(type);
        }

        private static void Register(int id, Func<IPacket> constructor)
        {
            var type = constructor().GetType();

            _constructors[id] = constructor;
            _typeToId[type] = id;

            DebugConsole.LogSuccess($"[PacketRegistry] Registered {type.Name} => {id}");
        }

        public static IPacket Create(int type)
		{
			return _constructors.TryGetValue(type, out var ctor)
					? ctor()
					: throw new InvalidOperationException($"No packet registered for type {type}");
		}

        public static int GetPacketId(IPacket packet)
        {
            var type = packet.GetType();

            if (!_typeToId.TryGetValue(type, out int id))
                throw new InvalidOperationException($"Packet type {type.Name} is not registered");

            return id;
        }

        public static void RegisterDefaults()
		{
            TryRegister(() => new ChoreAssignmentPacket());
            TryRegister(() => new EntityPositionPacket());
            TryRegister(() => new ChatMessagePacket());
            TryRegister(() => new WorldDataPacket());
            TryRegister(() => new WorldDataRequestPacket());
            TryRegister(() => new WorldUpdatePacket());
            TryRegister(() => new NavigatorPathPacket());
            TryRegister(() => new SaveFileRequestPacket());
            TryRegister(() => new SaveFileChunkPacket());
            TryRegister(() => new DiggablePacket());
            TryRegister(() => new DigCompletePacket());
            TryRegister(() => new PlayAnimPacket());
            TryRegister(() => new BuildPacket());
            TryRegister(() => new BuildCompletePacket());
            TryRegister(() => new WorldDamageSpawnResourcePacket());
            TryRegister(() => new WorldCyclePacket());
            TryRegister(() => new CancelPacket());
            TryRegister(() => new DeconstructPacket());
            TryRegister(() => new DeconstructCompletePacket());
            TryRegister(() => new UtilityBuildPacket(), "UtilityBuildPacket (WireBuild)");
            TryRegister(() => new ToggleMinionEffectPacket());
            TryRegister(() => new ToolEquipPacket());
            TryRegister(() => new DuplicantConditionPacket());
            TryRegister(() => new MoveToLocationPacket());
            TryRegister(() => new PrioritizePacket());
            TryRegister(() => new ClearPacket());
            TryRegister(() => new ClientReadyStatusPacket());
            TryRegister(() => new ClientReadyStatusUpdatePacket());
            TryRegister(() => new AllClientsReadyPacket());
            TryRegister(() => new EventTriggeredPacket());
            TryRegister(() => new HardSyncPacket());
            TryRegister(() => new HardSyncCompletePacket());
            TryRegister(() => new DisinfectPacket());
            TryRegister(() => new SpeedChangePacket());
            TryRegister(() => new PlayerCursorPacket());
            TryRegister(() => new BuildingStatePacket());
            TryRegister(() => new DiggingStatePacket());
            TryRegister(() => new ChoreStatePacket());
            TryRegister(() => new ResearchStatePacket());
            TryRegister(() => new PrioritizeStatePacket());
            TryRegister(() => new DisinfectStatePacket());
            TryRegister(() => new DuplicantStatePacket());
            TryRegister(() => new StructureStatePacket());
            TryRegister(() => new ResearchRequestPacket());
            TryRegister(() => new BuildingConfigPacket());
            TryRegister(() => new ImmigrantOptionsPacket());
            TryRegister(() => new ImmigrantSelectionPacket());
            TryRegister(() => new DuplicantPriorityPacket());
            TryRegister(() => new SkillMasteryPacket());
            TryRegister(() => new ScheduleUpdatePacket());
            TryRegister(() => new ScheduleAssignmentPacket());
            TryRegister(() => new FallingObjectPacket());
            TryRegister(() => new ConsumablePermissionPacket());
            TryRegister(() => new VitalStatsPacket());
            TryRegister(() => new ResourceCountPacket());
            TryRegister(() => new NotificationPacket());
            TryRegister(() => new ScheduleDeletePacket());
            TryRegister(() => new ConsumableStatePacket());
            TryRegister(() => new ResearchProgressPacket());
            TryRegister(() => new ResearchCompletePacket());
            TryRegister(() => new EntitySpawnPacket());
		}

        public static void TryRegister(Func<IPacket> constructor, string nameOverride = "")
        {
            try
            {
                Register(nextId, constructor);
                nextId++;
            }
            catch (Exception e)
            {
                string name = string.IsNullOrEmpty(nameOverride)
                    ? constructor.GetType().Name
                    : nameOverride;

                DebugConsole.LogError($"Failed to register {name}: {e}");
            }
        }
    }
}
