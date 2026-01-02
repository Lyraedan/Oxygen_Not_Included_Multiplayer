using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;
using UnityEngine;

namespace ONI_MP.Patches.World.SideScreen
{
	/// <summary>
	/// Generic sync helpers for Side Screen UI components.
	/// Used by all side screen patches to send building config packets.
	/// </summary>
	public static class SideScreenSyncHelper
	{
		public static void SyncSliderChange(Component target, float value, int sliderIndex = 0)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (target == null) return;

			var identity = target.GetComponent<NetworkIdentity>();
			if (identity == null)
			{
				identity = target.gameObject.AddOrGet<NetworkIdentity>();
				identity.RegisterIdentity();
			}

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target.gameObject),
				ConfigHash = "Slider".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.SliderIndex,
				SliderIndex = sliderIndex
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncThresholdChange(GameObject target, float value)
		{
			DebugConsole.Log($"[SideScreenSyncHelper.SyncThresholdChange] Called for {target?.name ?? "null"}, value={value}");
			
			if (BuildingConfigPacket.IsApplyingPacket)
			{
				DebugConsole.Log("[SideScreenSyncHelper.SyncThresholdChange] IsApplyingPacket=true, skipping");
				return;
			}
			if (target == null) return;
			
			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "Threshold".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.Float
			};

			DebugConsole.Log($"[SideScreenSyncHelper.SyncThresholdChange] Sending packet: ConfigHash={packet.ConfigHash}, Value={value}");

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncThresholdDirection(GameObject target, bool activateAbove)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (target == null) return;

			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "ThresholdDir".GetHashCode(),
				Value = activateAbove ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncCheckboxChange(GameObject target, bool value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (target == null) return;

			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "Checkbox".GetHashCode(),
				Value = value ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncCapacityChange(GameObject target, float value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (target == null) return;

			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "Capacity".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncDoorState(GameObject target, Door.ControlState state)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (target == null) return;

			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "DoorState".GetHashCode(),
				Value = (float)(int)state,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

        public static void SyncQueueBuildingEnabledToggle(GameObject target, bool queuedToggle)
        {
            if (BuildingConfigPacket.IsApplyingPacket) return;
            if (target == null) return;

            var identity = target.AddOrGet<NetworkIdentity>();
            identity.RegisterIdentity();

            var packet = new BuildingConfigPacket
            {
                NetId = identity.NetId,
                Cell = Grid.PosToCell(target),
                ConfigHash = "QueueBuildingEnableStateChange".GetHashCode(),
                Value = queuedToggle ? 1f : 0f,
                ConfigType = BuildingConfigType.Boolean
            };

            DebugConsole.Log($"[SideScreenSyncHelper.SyncQueueBuildingEnabledToggle] Sending packet: ConfigHash={packet.ConfigHash}, Value={packet.Value}");

            if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
            else PacketSender.SendToHost(packet);
        }

        public static void SyncBuildingEnabledChange(GameObject target, bool buildingEnabled)
        {
            if (BuildingConfigPacket.IsApplyingPacket) return;
            if (target == null) return;

            var identity = target.AddOrGet<NetworkIdentity>();
            identity.RegisterIdentity();

            var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "BuildingEnableStateChange".GetHashCode(),
				Value = buildingEnabled ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

            DebugConsole.Log($"[SideScreenSyncHelper.SyncBuildingEnabledChange] Sending packet: ConfigHash={packet.ConfigHash}, Value={packet.Value}");

            // Only hosts sync building enabled state to clients
            if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
		}
    }
}
