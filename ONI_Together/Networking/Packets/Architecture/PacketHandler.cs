using System;
using System.IO;
using ONI_Together.DebugTools;
using ONI_Together.Networking;
using ONI_Together.Networking.Overlay;
using ONI_Together.Networking.Packets.Core;
using ONI_Together.Networking.Packets.Handshake;
using ONI_Together.Networking.Packets.Social;
using ONI_Together.Networking.Packets.Tools;
using ONI_Together.Networking.Packets.World;
using ONI_Together.Misc;
using System.Collections.Generic;
using Shared.Profiling;
using UnityEngine;

namespace ONI_Together.Networking.Packets.Architecture
{

	public static class PacketHandler
	{
		private static bool _readyToProcess = true;

		/// <summary>
		/// The packets a client may act on while it has no world - in the frontend, waiting
		/// for or downloading the host's save.
		///
		/// readyToProcess is switched on in the menu on purpose (GameClient.ContinueConnectionFlow),
		/// because the save transfer itself arrives as packets. But the host does not filter
		/// what it broadcasts by a client's readiness: SendToAllClients walks every connected
		/// player, and MultiplayerPlayer.readyState is tracked but never consulted when
		/// sending. So a client in the menu receives the live world - effect toggles for
		/// duplicants it does not have, prefab spawns into a grid that does not exist,
		/// tool packets against no buildings. With Grid.WidthInCells at zero those land as
		/// DivideByZeroException inside Grid.CellToPos, NullReferenceException in
		/// Workable.OnSpawn, Prioritizable.OnSpawn and SaveLoadRoot.OnSpawn, and a
		/// DivideByZeroException in KBatchedAnimUpdater every frame until the world loads.
		/// Measured on a client two seconds into a save download: 224 effect toggles for
		/// missing minions, seven prefab spawns into no world, 76 anim-updater frames.
		///
		/// Everything outside this set is dropped, not queued. That is safe: HandleIncoming
		/// already drops rather than queues while not ready, and the state those packets
		/// describe is in the save the client is about to load.
		///
		/// BulkSenderPacket and ChunkedPacket are containers. Chunked re-enters
		/// HandleIncoming, so it is gated by the same check; Bulk dispatches its inner
		/// packets directly and applies ShouldDispatchWithoutWorld to each of them itself.
		/// </summary>
		private static readonly HashSet<Type> AllowedWithoutWorld = new HashSet<Type>
		{
			// connection flow and readiness
			typeof(GameStateRequestPacket),
			typeof(ClientReadyStatusPacket),
			typeof(ClientReadyStatusUpdatePacket),
			typeof(AllClientsReadyPacket),
			typeof(HardSyncPacket),
			typeof(HardSyncCompletePacket),
			typeof(HostBroadcastPacket),
			typeof(DedicatedServerMessagePacket),
			typeof(DisconnectPacket),
			typeof(PingPacket),
			// save transfer
			typeof(SaveFileRequestPacket),
			typeof(SaveFileChunkPacket),
			typeof(ChunkAckPacket),
			typeof(TcpTransferStartPacket),
			typeof(TcpFallbackRequestPacket),
			typeof(SecureTransferPacket),
			typeof(SyncProgressPacket),
			typeof(WorldDataPacket),
			typeof(WorldDataRequestPacket),
			// chat is harmless without a world
			typeof(ChatMessagePacket),
			typeof(ChatHistorySyncPacket),
			// containers
			typeof(BulkSenderPacket),
			typeof(ChunkedPacket),
		};

		/// <summary>
		/// True when this packet may run on the local side right now. Only a client with no
		/// world is restricted; a host, or a client in a loaded world, dispatches everything.
		/// </summary>
		public static bool ShouldDispatchWithoutWorld(IPacket packet)
		{
			using var _ = Profiler.Scope();

			if (MultiplayerSession.IsHost) return true;
			if (!Utils.IsInMenu()) return true;
			return AllowedWithoutWorld.Contains(packet.GetType());
		}
		private static float _notReadySince = float.MaxValue;
		private const float NOT_READY_TIMEOUT = 60f;

		public static bool readyToProcess
		{
			get => _readyToProcess;
			set
			{
				if (!value)
					_notReadySince = Time.unscaledTime;
				_readyToProcess = value;
			}
		}

		public static void HandleIncoming(byte[] data)
		{
			using var _ = Profiler.Scope();

			if (!_readyToProcess)
			{
				if (Time.unscaledTime - _notReadySince > NOT_READY_TIMEOUT)
				{
					DebugConsole.LogWarning($"[PacketHandler] readyToProcess was false for >{NOT_READY_TIMEOUT}s — force-recovering");
					_readyToProcess = true;
				}
				else
				{
					return;
				}
			}

			using (var ms = new MemoryStream(data))
			{
				using (var reader = new BinaryReader(ms))
				{
					int type = (int)reader.ReadInt32();
                    if (!PacketRegistry.HasRegisteredPacket(type))
                    {
                        DebugConsole.LogError($"Invalid PacketType received: {type}", false);
                        return;
                    }

                    using var scope = Profiler.Scope();

                    var packet = PacketRegistry.Create(type);
					packet.Deserialize(reader);

					if (!ShouldDispatchWithoutWorld(packet))
						return;

					Dispatch(packet);

                    scope.End(packet.GetType().Name, data.Length);

                    PacketTracker.TrackIncoming(new PacketTracker.PacketTrackData
                    {
						packet = packet,
						size = data.Length
                    });

					var tracker = NetIdActivityTracker.Instance;
					if (tracker != null)
					{
						int netId = NetIdActivityTracker.GetNetIdFromPacket(packet);
						if (netId > 0)
							tracker.RecordActivity(netId, data.Length);
					}
                }
			}
		}

		private static void Dispatch(IPacket packet)
		{
			using var _ = Profiler.Scope();

			packet.OnDispatched();
		}
	}

}