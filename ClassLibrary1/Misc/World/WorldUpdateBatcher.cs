using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using System;
using System.Collections.Generic;
using ONI_MP.Profiling;
using UnityEngine;

namespace ONI_MP.Misc.World
{
	public static class WorldUpdateBatcher
	{
		private static readonly List<WorldUpdatePacket.CellUpdate> pendingUpdates = new List<WorldUpdatePacket.CellUpdate>();
		private static float flushTimer = 0f;
		private const float FlushInterval = 10f; // Seconds

		public static void Queue(WorldUpdatePacket.CellUpdate update)
		{
			Profiler.Active.Scope();

			if(MultiplayerSession.IsClient)
			{
				// Client is not allowed to send WorldUpdate states as the host has full authority
				return;
			}

			lock (pendingUpdates)
			{
				pendingUpdates.Add(update);
			}
		}

		public static void Update()
		{
			Profiler.Active.Scope();

			if (MultiplayerSession.IsClient)
			{
				return;
			}

			flushTimer += Time.unscaledDeltaTime;
			if (flushTimer >= FlushInterval)
			{
				Flush();
				flushTimer = 0f;
			}
		}

		public static int Flush()
		{
			Profiler.Active.Scope();

			if (MultiplayerSession.IsClient)
			{
				return 0;
			}

            lock (pendingUpdates)
			{
				if (pendingUpdates.Count == 0)
					return 0;

				if(MultiplayerSession.IsClient)
				{
					// This should never happen, but its better to be safe then sorry
                    pendingUpdates.Clear();
                    return 0;
				}

				int totalUpdates = pendingUpdates.Count;
				
				// Each cell update is roughly 5.38 bytes after compression (1KB / 5.38 = 188)
				const int MaxUpdatesPerPacket = 180; // Keep packet size under ~1KB

				for (int i = 0; i < pendingUpdates.Count; i += MaxUpdatesPerPacket)
				{
					var chunk = pendingUpdates.GetRange(i, Math.Min(MaxUpdatesPerPacket, pendingUpdates.Count - i));
					var packet = new WorldUpdatePacket();
					packet.Updates.AddRange(chunk);
					PacketSender.SendToAllClients(packet, sendType: SteamNetworkingSend.Unreliable); // max packet size 1200 bytes (typically 1170–1200 bytes)
                }

				pendingUpdates.Clear();
				
				// Return estimated packet size (5.38 bytes per update)
				return (int)(totalUpdates * 5.38f);
			}
		}

	}
}
