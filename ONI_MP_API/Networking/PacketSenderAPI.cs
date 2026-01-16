using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Helpers;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP_API.Networking
{
	public static class PacketSenderAPI
	{
		static bool Init()
		{
			if (typesInitialized)
				return true;

			if (!ReflectionHelper.TryCreateDelegate<SendToAllDelegate>("ONI_MP.Networking.PacketSender, ONI_MP", "SendToAll_API", [typeof(object), typeof(ulong?), typeof(int)], out _SendToAll))
				return false;

			if (!ReflectionHelper.TryCreateDelegate<SendToAllClientsDelegate>("ONI_MP.Networking.PacketSender, ONI_MP", "SendToAllClients_API", [typeof(object), typeof(int)], out _SendToAllClients))
				return false;

			if (!ReflectionHelper.TryCreateDelegate<SendToAllExcludingDelegate>("ONI_MP.Networking.PacketSender, ONI_MP", "SendToAllExcluding_API", [typeof(object), typeof(HashSet<ulong>), typeof(int)], out _SendToAllExcluding))
				return false;

			if (!ReflectionHelper.TryCreateDelegate<SendToPlayerDelegate>("ONI_MP.Networking.PacketSender, ONI_MP", "SendToPlayer_API", [typeof(ulong), typeof(object), typeof(int)], out _SendToPlayer))
				return false;

			if (!ReflectionHelper.TryCreateDelegate<SendToHostDelegate>("ONI_MP.Networking.PacketSender, ONI_MP", "SendToHost_API", [typeof(object), typeof(int)], out _SendToHost))
				return false;

			if (!ReflectionHelper.TryCreateDelegate<SendToAllOtherPeersDelegate>("ONI_MP.Networking.PacketSender, ONI_MP", "SendToAllOtherPeers_API", [typeof(object)], out _SendToAllOtherPeers))
				return false;

			typesInitialized = true;
			return true;
		}

		static bool typesInitialized = false;

		static SendToAllDelegate? _SendToAll = null;
		delegate void SendToAllDelegate(object packet, ulong? exclude = null, int sendType = (int)SteamNetworkingSend.Reliable);

		static SendToAllClientsDelegate? _SendToAllClients = null;
		delegate void SendToAllClientsDelegate(object packet, int sendType = (int)SteamNetworkingSend.Reliable);

		static SendToAllExcludingDelegate? _SendToAllExcluding = null;
		delegate void SendToAllExcludingDelegate(object packet, HashSet<ulong> excludedIds, int sendType = (int)SteamNetworkingSend.Reliable);

		static SendToPlayerDelegate? _SendToPlayer = null;
		delegate void SendToPlayerDelegate(ulong steamID, object packet, int sendType = (int)SteamNetworkingSend.ReliableNoNagle);

		static SendToHostDelegate? _SendToHost = null;
		delegate void SendToHostDelegate(object packet, int sendType = (int)SteamNetworkingSend.ReliableNoNagle);

		static SendToAllOtherPeersDelegate? _SendToAllOtherPeers = null;
		delegate void SendToAllOtherPeersDelegate(object packet);

		/// Original single-exclude overload
		public static void SendToAll(IPacket packet, ulong? exclude = null, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			Init();
			if (_SendToAll == null)
				return;
			_SendToAll(packet, exclude, (int)sendType);
		}

		public static void SendToAllClients(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			Init();
			if (_SendToAllClients == null)
				return;
			_SendToAllClients(packet, (int)sendType);
		}

		public static void SendToAllExcluding(IPacket packet, HashSet<ulong> excludedIds, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			Init();
			if (_SendToAllExcluding == null)
				return;
			_SendToAllExcluding(packet, excludedIds, (int)sendType);
		}

		public static void SendToPlayer(ulong steamId, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
		{
			Init();
			if (_SendToPlayer == null)
				return;
			_SendToPlayer(steamId, packet, (int)sendType);
		}

		public static void SendToHost(IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
		{
			Init();
			if (_SendToHost == null)
				return;
			_SendToHost(packet, (int)sendType);
		}
		public static void SendToAllOtherPeers(IPacket packet)
		{
			Init();
			if (_SendToAllOtherPeers == null)
				return;
			_SendToAllOtherPeers(packet);
		}
	}
}
