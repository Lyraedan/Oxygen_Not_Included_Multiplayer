using ONI_MP.Profiling;
using UnityEngine;

namespace ONI_MP.DebugTools
{
	/// <summary>
	/// Centralized sync metrics for debug display.
	/// Each syncer updates its metric after performing a sync operation.
	/// </summary>
	public static class SyncStats
	{
		public class SyncMetric
		{
			public string Name;
			public float Interval;
			public float LastSyncTime;
			public int LastItemCount;
			public int LastPacketBytes;
			public float LastDurationMs;

			public float TimeRemaining => Mathf.Max(0, Interval - (Time.unscaledTime - LastSyncTime));
		}

		// WorldStateSyncer metrics
		public static SyncMetric Gas = new SyncMetric { Name = "Gas/Liquid", Interval = 1.5f };
		public static SyncMetric Digging = new SyncMetric { Name = "Digging", Interval = 3f };
		public static SyncMetric Chores = new SyncMetric { Name = "Chores", Interval = 3f };
		public static SyncMetric Research = new SyncMetric { Name = "Research", Interval = 3f };
		// Priorities and Disinfect removed - synced via event-driven patches

		// Other syncer metrics
		public static SyncMetric Buildings = new SyncMetric { Name = "Buildings", Interval = 30f };
		public static SyncMetric Structures = new SyncMetric { Name = "Structures", Interval = 0.5f };
		public static SyncMetric VitalStats = new SyncMetric { Name = "VitalStats", Interval = 1f };

		/// <summary>
		/// Updates a metric after a sync operation.
		/// </summary>
		public static void RecordSync(SyncMetric metric, int itemCount, int packetBytes, float durationMs)
		{
			Profiler.Active.Scope();

			metric.LastSyncTime = Time.unscaledTime;
			metric.LastItemCount = itemCount;
			metric.LastPacketBytes = packetBytes;
			metric.LastDurationMs = durationMs;
		}

		/// <summary>
		/// All metrics for iteration in debug display.
		/// </summary>
		public static SyncMetric[] AllMetrics => new[]
		{
			Gas, Digging, Chores, Research,
			Buildings, Structures, VitalStats
		};
	}
}
