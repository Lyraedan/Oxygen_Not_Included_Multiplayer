using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using System.Collections.Generic;
using ONI_MP.Profiling;
using UnityEngine;

namespace ONI_MP.Misc.World
{
	public static class InstantiationBatcher
	{
		private static readonly List<InstantiationsPacket.InstantiationEntry> queue = new List<InstantiationsPacket.InstantiationEntry>();
		private static float timeSinceLastFlush = 0f;
		private const float FlushInterval = 2.0f;

		public static void Queue(InstantiationsPacket.InstantiationEntry entry)
		{
			Profiler.Active.Scope();

			queue.Add(entry);
		}

		public static void Update()
		{
			Profiler.Active.Scope();

			timeSinceLastFlush += Time.unscaledDeltaTime;

			if (timeSinceLastFlush >= FlushInterval)
			{
				Flush();
				timeSinceLastFlush = 0f;
			}
		}

		public static void Flush()
		{
			Profiler.Active.Scope();

			if (queue.Count == 0)
				return;

			var packet = new InstantiationsPacket
			{
				Entries = new List<InstantiationsPacket.InstantiationEntry>(queue)
			};

			PacketSender.SendToAll(packet, sendType: SteamNetworkingSend.Unreliable);
			queue.Clear();
		}
	}
}
