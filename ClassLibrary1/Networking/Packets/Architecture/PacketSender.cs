using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ONI_MP.Networking
{
	public static class PacketSender
	{
		public static int MAX_PACKET_SIZE_RELIABLE = 512;
		public static int MAX_PACKET_SIZE_UNRELIABLE = 1024;

		public static byte[] SerializePacket(IPacket packet)
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

		/// <summary>
		/// Send to one connection by HSteamNetConnection handle.
		/// </summary>
		public static bool SendToConnection(HSteamNetConnection conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
		{
			var bytes = SerializePacket(packet);
			var _sendType = (int)sendType;

			IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
			try
			{
				Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);

				var result = SteamNetworkingSockets.SendMessageToConnection(
						conn, unmanagedPointer, (uint)bytes.Length, _sendType, out long msgNum);

				bool sent = result == EResult.k_EResultOK;

				if (!sent)
				{
					// DebugConsole.LogError($"[Sockets] Failed to send {packet.Type} to conn {conn} ({Utils.FormatBytes(bytes.Length)} | result: {result})", false);
				}
				else
				{
					PacketTracker.TrackSent(new PacketTracker.PacketTrackData {
						packet = packet,
						size = bytes.Length
					});
					//DebugConsole.Log($"[Sockets] Sent {packet.Type} to conn {conn} ({Utils.FormatBytes(bytes.Length)})");
				}
				return sent;
			}
			finally
			{
				Marshal.FreeHGlobal(unmanagedPointer);
			}
		}

		/// <summary>
		/// Send a packet to a player by their SteamID.
		/// </summary>
		public static bool SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
		{
			// Prevent host from sending packets to itself (can cause loops and errors)
			if (MultiplayerSession.IsHost && steamID == MultiplayerSession.HostSteamID)
			{
				DebugConsole.LogWarning($"[PacketSender] Host attempted to send packet {packet.GetType().Name} to itself - blocked");
				return false;
			}

			if (!MultiplayerSession.ConnectedPlayers.TryGetValue(steamID, out var player) || player.Connection == null)
			{
				DebugConsole.LogWarning($"[PacketSender] No connection found for SteamID {steamID}");
				return false;
			}

			return SendToConnection(player.Connection.Value, packet, sendType);
		}

		public static void SendToHost(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
		{
			if (!MultiplayerSession.HostSteamID.IsValid())
			{
				DebugConsole.LogWarning($"[PacketSender] Failed to send to host. Host is invalid.");
				return;
			}
			SendToPlayer(MultiplayerSession.HostSteamID, packet, sendType);
		}

		/// Original single-exclude overload
		public static void SendToAll(IPacket packet, CSteamID? exclude = null, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
			{
				if (exclude.HasValue && player.SteamID == exclude.Value)
					continue;

				if (player.Connection != null)
					SendToConnection(player.Connection.Value, packet, sendType);
			}
		}

		public static void SendToAllClients(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			if (!MultiplayerSession.IsHost)
			{
				DebugConsole.LogWarning("[PacketSender] Only the host can send to all clients");
				return;
			}
			SendToAll(packet, MultiplayerSession.HostSteamID, sendType);
		}

		public static void SendToAllExcluding(IPacket packet, HashSet<CSteamID> excludedIds, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
			{
				if (excludedIds != null && excludedIds.Contains(player.SteamID))
					continue;

				if (player.Connection != null)
					SendToConnection(player.Connection.Value, packet, sendType);
			}
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
				SendToHost(new HostBroadcastPacket(packet, MultiplayerSession.LocalSteamID));
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
		public static void SendToAll_API(object api_packet, CSteamID? exclude = null, int sendType = (int)SteamNetworkingSend.Reliable)
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

		public static void SendToAllExcluding_API(object api_packet, HashSet<CSteamID> excludedIds, int sendType = (int)SteamNetworkingSend.Reliable)
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

		public static void SendToPlayer_API(CSteamID steamID, object api_packet, int sendType = (int)SteamNetworkingSend.ReliableNoNagle)
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
