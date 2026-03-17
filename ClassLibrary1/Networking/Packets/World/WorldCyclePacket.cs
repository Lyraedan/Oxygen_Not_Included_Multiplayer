using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.GamePatches;
using System.IO;
using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.World
{
	public class WorldCyclePacket : IPacket
	{
		public int Cycle { get; set; }
		public float CycleTime { get; set; }

		public void Serialize(BinaryWriter writer)
		{
			Profiler.Active.Scope();

			writer.Write(Cycle);
			writer.Write(CycleTime);
		}

		public void Deserialize(BinaryReader reader)
		{
			Profiler.Active.Scope();

			Cycle = reader.ReadInt32();
			CycleTime = reader.ReadSingle();
		}

		public void OnDispatched()
		{
			Profiler.Active.Scope();

			if (MultiplayerSession.IsHost)
				return;

			float totalTime = Cycle * 600f + CycleTime;

			if (GameClock.Instance != null)
			{
				GameClockPatch.allowAddTimeForSetTime = true;
				GameClock.Instance.SetTime(totalTime);
				GameClockPatch.allowAddTimeForSetTime = false;
			}
			else
			{
				DebugConsole.LogWarning("[Multiplayer] GameClock.Instance is null — cannot apply cycle sync.");
			}
		}
	}
}
