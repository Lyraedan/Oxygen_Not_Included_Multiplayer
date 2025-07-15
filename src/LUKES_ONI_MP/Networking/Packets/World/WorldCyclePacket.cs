using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.GamePatches;
using UnityEngine;

namespace ONI_MP.Networking.Packets.World
{
    public class WorldCyclePacket : IPacket
    {
        public PacketType Type => PacketType.WorldCycle;

        public int Cycle { get; set; }
        public float CycleTime { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Cycle);
            writer.Write(CycleTime);
        }

        public void Deserialize(BinaryReader reader)
        {
            Cycle = reader.ReadInt32();
            CycleTime = reader.ReadSingle();
        }

        public void OnDispatched()
        {
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
