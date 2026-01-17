using Epic.OnlineServices.P2P;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.Relay;
using ONI_MP.Networking.Relay.Steam;
using Shared.Interfaces.Networking;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ONI_MP.Networking
{
	public static class PacketSender
	{
		/// <summary>
		/// Sth in this is broken
		/// </summary>
		private class PacketUpdateRunner
		{
			int PacketId; 
			float UpdateIntervalS;

			Dictionary<HSteamNetConnection, float> LastDispatchTime = [];
			
			public PacketUpdateRunner(int packetId, uint updateInterval)
			{
				PacketId = packetId;
				UpdateIntervalS = updateInterval/1000f;
			}
			public bool CanDispatchNext(HSteamNetConnection connection)
			{
				var currentTime = Time.unscaledTime;

				if (!LastDispatchTime.ContainsKey(connection))
				{
					LastDispatchTime[connection] = currentTime;
					return true;
				}

				if (LastDispatchTime[connection] + UpdateIntervalS > currentTime)
				{
					LastDispatchTime[connection] = currentTime;
					return true;
				}
				return false;
			}
		}


		public static int MAX_PACKET_SIZE_RELIABLE = 512;
		public static int MAX_PACKET_SIZE_UNRELIABLE = 1024;
		public static RelayPacketSender relaySender = new SteamPacketSender();

		public static byte[] SerializePacketForSending(IPacket packet)
		{
			using (var ms = new System.IO.MemoryStream())
			using (var writer = new System.IO.BinaryWriter(ms))
			{
				int packet_type = PacketRegistry.GetPacketId(packet);
				writer.Write(packet_type);
				packet.Serialize(writer);
				return ms.ToArray();
			}
		}

		static Dictionary<int, PacketUpdateRunner> UpdateRunners = [];
		static Dictionary<object, Dictionary<int, List<byte[]>>> WaitingBulkPacketsPerReceiver = [];
		public static void DispatchPendingBulkPackets()
		{
			foreach (var kvp in WaitingBulkPacketsPerReceiver)
			{
				var conn = kvp.Key;
				foreach (var packetId in kvp.Value.Keys)
				{
					DispatchPendingBulkPacketOfType(conn, packetId, true);
				}
			}
		}

		static void DispatchPendingBulkPacketOfType(object conn, int packetId, bool intervalRun = false)
		{
			if (!WaitingBulkPacketsPerReceiver.TryGetValue(conn, out var allPendingPackets)
				|| !allPendingPackets.TryGetValue(packetId, out var pendingPackets)
				|| !pendingPackets.Any())
			{
				return;
			}
			//if (intervalRun)
			//{
			//	if (!UpdateRunners[packetId].CanDispatchNext(conn))
			//		return;
			//}
			SendToConnection(conn, new BulkSenderPacket(packetId, pendingPackets), SteamNetworkingSend.ReliableNoNagle);
			pendingPackets.Clear();
		}
		public static void AppendPendingBulkPacket(object conn, IPacket packet, IBulkablePacket bp)
		{
			int packetId = PacketRegistry.GetPacketId(packet);
			int maxPacketNumberPerPacket = bp.MaxPackSize;

			if (!UpdateRunners.ContainsKey(packetId))
			{
				UpdateRunners[packetId] = new PacketUpdateRunner(packetId, bp.IntervalMs);
			}

			if (!WaitingBulkPacketsPerReceiver.TryGetValue(conn, out var bulkPacketWaitingData))
			{
				WaitingBulkPacketsPerReceiver[conn] = [];
				bulkPacketWaitingData = WaitingBulkPacketsPerReceiver[conn];
			}
			if (!bulkPacketWaitingData.TryGetValue(packetId, out var pendingPackets))
			{
				bulkPacketWaitingData[packetId] = new List<byte[]>(maxPacketNumberPerPacket);
				pendingPackets = bulkPacketWaitingData[packetId];
			}
			pendingPackets.Add(packet.SerializeToByteArray());
			if (pendingPackets.Count >= maxPacketNumberPerPacket)
			{
				DispatchPendingBulkPacketOfType(conn, packetId);
			}
		}
		public static byte[] SerializeToByteArray(this IPacket packet)
		{
			using var ms = new System.IO.MemoryStream();
			using var writer = new System.IO.BinaryWriter(ms);
			packet.Serialize(writer);
			return ms.ToArray();
		}

		/// <summary>
		/// Send to one connection by HSteamNetConnection handle.
		/// </summary>
		/// 

		public static bool SendToConnection(object conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
		{
			if (packet is IBulkablePacket bp)
			{
				AppendPendingBulkPacket(conn, packet, bp);
				return true;
			}

			return NetworkConfig.RelayPacketSender.SendToConnection(conn, packet, sendType);
		}

		/// <summary>
		/// Send a packet to a player by their SteamID.
		/// </summary>
		public static bool SendToPlayer(ulong steamID, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
		{
			// Prevent host from sending packets to itself (can cause loops and errors)
			if (MultiplayerSession.IsHost && steamID == MultiplayerSession.HostUserID)
			{
				DebugConsole.LogWarning($"[PacketSender] Host attempted to send packet {packet.GetType().Name} to itself - blocked");
				return false;
			}

			if (!MultiplayerSession.ConnectedPlayers.TryGetValue(steamID, out var player) || player.Connection == null)
			{
				DebugConsole.LogWarning($"[PacketSender] No connection found for SteamID {steamID}");
				return false;
			}

			return SendToConnection(player.Connection, packet, sendType);
		}

		public static void SendToHost(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
		{
			if (!MultiplayerSession.HostUserID.IsValid())
			{
				DebugConsole.LogWarning($"[PacketSender] Failed to send to host. Host is invalid.");
				return;
			}
			SendToPlayer(MultiplayerSession.HostUserID, packet, sendType);
		}

		/// Original single-exclude overload
		public static void SendToAll(IPacket packet, ulong? exclude = null, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
			{
				if (exclude.HasValue && player.SteamID == exclude.Value)
					continue;

				if (player.Connection != null)
					SendToConnection(player.Connection, packet, sendType);
			}
		}

		public static void SendToAllClients(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			if (!MultiplayerSession.IsHost)
			{
				DebugConsole.LogWarning("[PacketSender] Only the host can send to all clients. Tried sending: " + packet.GetType());
				return;
			}
			SendToAll(packet, MultiplayerSession.HostUserID, sendType);
		}

		public static void SendToAllExcluding(IPacket packet, HashSet<ulong> excludedIds, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
			{
				if (excludedIds != null && excludedIds.Contains(player.SteamID))
					continue;

				if (player.Connection != null)
					SendToConnection(player.Connection, packet, sendType);
			}
		}

		/// <summary>
		/// Sends a packet to all other players.
		/// Forces the packet origin to be on the host itself
		/// if sent from the host, it goes to all clients.
		/// otherwise it is wrapped in a HostBroadcastPacket and sent to the host for rebroadcasting.
		/// 
		/// </summary>
		/// <param name="packet"></param>
		public static void SendToAllOtherPeersFromHost(IPacket packet)
		{
			if (!MultiplayerSession.InSession)
			{
				DebugConsole.LogWarning("[PacketSender] Not in a multiplayer session, cannot send to other peers");
				return;
			}
			DebugConsole.Log("[PacketSender] Sending packet to all other peers: " + packet.GetType().Name + " from host");

			if (MultiplayerSession.IsHost)
				SendToAllClients(packet);
			else
				SendToHost(new HostBroadcastPacket(packet, Utils.NilUlong()));
		}

		public static void SendToAllOtherPeersFromHost_API(object api_packet)
		{
			var type = api_packet.GetType();
			if (!PacketRegistry.HasRegisteredPacket(type))
			{
				DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
				return;
			}
			if (!API_Helper.WrapApiPacket(api_packet, out var packet))
			{
				DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
				return;
			}
			SendToAllOtherPeersFromHost(packet);
		}


		/// <summary>
		/// Sends a packet to all other players.
		/// if sent from the host, it goes to all clients.
		/// otherwise it is wrapped in a HostBroadcastPacket and sent to the host for rebroadcasting.
		/// </summary>
		/// <param name="packet"></param>
		public static void SendToAllOtherPeers(IPacket packet)
		{
			if (!MultiplayerSession.InSession)
			{
				DebugConsole.LogWarning("[PacketSender] Not in a multiplayer session, cannot send to other peers");
				return;
			}
			DebugConsole.Log("[PacketSender] Sending packet to all other peers: " + packet.GetType().Name);

			if (MultiplayerSession.IsHost)
				SendToAllClients(packet);
			else
				SendToHost(new HostBroadcastPacket(packet, MultiplayerSession.LocalUserID));
		}

		public static void SendToAllOtherPeers_API(object api_packet)
		{
			var type = api_packet.GetType();
			if (!PacketRegistry.HasRegisteredPacket(type))
			{
				DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
				return;
			}
			if (!API_Helper.WrapApiPacket(api_packet, out var packet))
			{
				DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
				return;
			}
			SendToAllOtherPeers(packet);
		}

		/// <summary>
		/// custom types, interfaces and enums are not directly usable across assembly boundaries
		/// </summary>
		/// <param name="api_packet">data object of the packet class that got registered with a ModApiPacket wrapper earlier</param>
		/// <param name="exclude"></param>
		/// <param name="sendType"></param>
		public static void SendToAll_API(object api_packet, ulong? exclude = null, int sendType = (int)SteamNetworkingSend.Reliable)
		{
			var type = api_packet.GetType();
			if (!PacketRegistry.HasRegisteredPacket(type))
			{
				DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
				return;
			}
			if (!API_Helper.WrapApiPacket(api_packet, out var packet))
			{
				DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
				return;
			}
			SendToAll(packet, exclude, (SteamNetworkingSend)sendType);
		}

		public static void SendToAllClients_API(object api_packet, int sendType = (int)SteamNetworkingSend.Reliable)
		{
			var type = api_packet.GetType();
			if (!PacketRegistry.HasRegisteredPacket(type))
			{
				DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
				return;
			}

			if (!API_Helper.WrapApiPacket(api_packet, out var packet))
			{
				DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
				return;
			}
			SendToAllClients(packet, (SteamNetworkingSend)sendType);
		}

		public static void SendToAllExcluding_API(object api_packet, HashSet<ulong> excludedIds, int sendType = (int)SteamNetworkingSend.Reliable)
		{
			var type = api_packet.GetType();
			if (!PacketRegistry.HasRegisteredPacket(type))
			{
				DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
				return;
			}

			if (!API_Helper.WrapApiPacket(api_packet, out var packet))
			{
				DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
				return;
			}
			SendToAllExcluding(packet, excludedIds, (SteamNetworkingSend)sendType);
		}

		public static void SendToPlayer_API(ulong steamID, object api_packet, int sendType = (int)SteamNetworkingSend.ReliableNoNagle)
		{
			var type = api_packet.GetType();
			if (!PacketRegistry.HasRegisteredPacket(type))
			{
				DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
				return;
			}

			if (!API_Helper.WrapApiPacket(api_packet, out var packet))
			{
				DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
				return;
			}
			SendToPlayer(steamID, packet, (SteamNetworkingSend)sendType);
		}

		public static void SendToHost_API(object api_packet, int sendType = (int)SteamNetworkingSend.ReliableNoNagle)
		{
			var type = api_packet.GetType();
			if (!PacketRegistry.HasRegisteredPacket(type))
			{
				DebugConsole.LogError($"[PacketSender] Attempted to send unregistered packet type: {type.Name}");
				return;
			}

			if (!API_Helper.WrapApiPacket(api_packet, out var packet))
			{
				DebugConsole.LogError($"[PacketSender] Failed to wrap API packet of type: {type.Name}");
				return;
			}
			SendToHost(packet, (SteamNetworkingSend)sendType);
		}

	}
}
