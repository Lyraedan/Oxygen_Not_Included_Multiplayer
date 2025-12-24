using HarmonyLib;
using KSerialization;
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
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ONI_MP.Networking.Packets.Architecture
{
	public static class PacketRegistry
	{
		private static readonly Dictionary<int, Type> _PacketTypes = new ();

        public static bool HasRegisteredPacket(int type)
        {
            return _PacketTypes.ContainsKey(type);
        }
		public static bool HasRegisteredPacket(Type type)
		{
			return _PacketTypes.ContainsKey(API_Helper.GetHashCode(type));
		}

		private static void Register(Type packageType)
        {
            int id = API_Helper.GetHashCode(packageType);
			var IPacketType = typeof(IPacket);
            if(IPacketType.IsAssignableFrom(packageType))
			{
				_PacketTypes[id] = packageType;
				DebugConsole.LogSuccess($"[PacketRegistry] Registered {packageType.Name} => {id}");
			}
			///Inheritance checks will fail for mod api packets, so these get wrapped in a generated type derived from ModApiPacket<T> at runtime
			else if (API_Helper.ValidAsModApiPacket(packageType))
            {
				///gotta register both ids so they can be created from either the wrapped or unwrapped type id
				var wrappedType = API_Helper.CreateModApiPacketType(packageType);
				_PacketTypes[id] = wrappedType;
				var wrappedId = API_Helper.GetHashCode(wrappedType);
				_PacketTypes[wrappedId] = wrappedType;
				DebugConsole.LogSuccess($"[PacketRegistry] Registered from ModAPI: {packageType.Name} => {id} (unwrapped), {wrappedId} (wrapped)");
			}
            else
                throw new InvalidOperationException($"Type {packageType.Name} does not implement IPacket interface");
        }
        public static IPacket Create(int type)
		{
			return _PacketTypes.TryGetValue(type, out var packetType)
					? (IPacket)Activator.CreateInstance(packetType)
					: throw new InvalidOperationException($"No packet registered for type {type}");
		}

        public static int GetPacketId(IPacket packet)
        {
            var type = packet.GetType();
            int id = API_Helper.GetHashCode(type);

			if (!_PacketTypes.TryGetValue(id, out _))
                throw new InvalidOperationException($"Packet type {type.Name} with id {id} is not registered");

            return id;
        }

        public static void RegisterDefaults()
		{
            TryRegister(typeof(ChoreAssignmentPacket));
            TryRegister(typeof(EntityPositionPacket));
            TryRegister(typeof(ChatMessagePacket));
            TryRegister(typeof(WorldDataPacket));
            TryRegister(typeof(WorldDataRequestPacket));
            TryRegister(typeof(WorldUpdatePacket));
            TryRegister(typeof(NavigatorPathPacket));
            TryRegister(typeof(SaveFileRequestPacket));
            TryRegister(typeof(SaveFileChunkPacket));
            TryRegister(typeof(DiggablePacket));
            TryRegister(typeof(DigCompletePacket));
            TryRegister(typeof(PlayAnimPacket));
            TryRegister(typeof(BuildPacket));
            TryRegister(typeof(BuildCompletePacket));
            TryRegister(typeof(WorldDamageSpawnResourcePacket));
            TryRegister(typeof(WorldCyclePacket));
            TryRegister(typeof(CancelPacket));
            TryRegister(typeof(DeconstructPacket));
            TryRegister(typeof(DeconstructCompletePacket));
            TryRegister(typeof(UtilityBuildPacket), "UtilityBuildPacket (WireBuild)");
            TryRegister(typeof(ToggleMinionEffectPacket));
            TryRegister(typeof(ToolEquipPacket));
            TryRegister(typeof(DuplicantConditionPacket));
            TryRegister(typeof(MoveToLocationPacket));
            TryRegister(typeof(PrioritizePacket));
            TryRegister(typeof(ClearPacket));
            TryRegister(typeof(ClientReadyStatusPacket));
            TryRegister(typeof(ClientReadyStatusUpdatePacket));
            TryRegister(typeof(AllClientsReadyPacket));
            TryRegister(typeof(EventTriggeredPacket));
            TryRegister(typeof(HardSyncPacket));
            TryRegister(typeof(HardSyncCompletePacket));
            TryRegister(typeof(DisinfectPacket));
            TryRegister(typeof(SpeedChangePacket));
            TryRegister(typeof(PlayerCursorPacket));
            TryRegister(typeof(BuildingStatePacket));
            TryRegister(typeof(DiggingStatePacket));
            TryRegister(typeof(ChoreStatePacket));
            TryRegister(typeof(ResearchStatePacket));
            TryRegister(typeof(PrioritizeStatePacket));
            TryRegister(typeof(DisinfectStatePacket));
            TryRegister(typeof(DuplicantStatePacket));
            TryRegister(typeof(StructureStatePacket));
            TryRegister(typeof(ResearchRequestPacket));
            TryRegister(typeof(BuildingConfigPacket));
            TryRegister(typeof(ImmigrantOptionsPacket));
            TryRegister(typeof(ImmigrantSelectionPacket));
            TryRegister(typeof(DuplicantPriorityPacket));
            TryRegister(typeof(SkillMasteryPacket));
            TryRegister(typeof(ScheduleUpdatePacket));
            TryRegister(typeof(ScheduleAssignmentPacket));
            TryRegister(typeof(FallingObjectPacket));
            TryRegister(typeof(ConsumablePermissionPacket));
            TryRegister(typeof(VitalStatsPacket));
            TryRegister(typeof(ResourceCountPacket));
            TryRegister(typeof(NotificationPacket));
            TryRegister(typeof(ScheduleDeletePacket));
            TryRegister(typeof(ConsumableStatePacket));
            TryRegister(typeof(ResearchProgressPacket));
            TryRegister(typeof(ResearchCompletePacket));
            TryRegister(typeof(EntitySpawnPacket));
            TryRegister(typeof(AssignmentPacket));
            TryRegister(typeof(ModVerificationPacket));
            TryRegister(typeof(ModVerificationResponsePacket));
            TryRegister(typeof(ModListRequestPacket));
		}

        public static void TryRegister(Type packetType, string nameOverride = "")
        {
            try
            {
                Register(packetType);
            }
            catch (Exception e)
            {
                string name = string.IsNullOrEmpty(nameOverride)
                    ? packetType.Name
                    : nameOverride;

                DebugConsole.LogError($"Failed to register {name}: {e}");
            }
        }
    }
}
