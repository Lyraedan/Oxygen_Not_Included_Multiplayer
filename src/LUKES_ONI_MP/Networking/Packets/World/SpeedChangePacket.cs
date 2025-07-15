using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.World
{
    public class SpeedChangePacket : IPacket
    {
        [Flags]
        public enum SpeedState : int
        {
            Paused = -1,
            Normal = 0,
            Double = 1,
            Triple = 2
        }

        public PacketType Type => PacketType.SpeedChange;

        public SpeedState Speed { get; set; }

        public SpeedChangePacket() { }

        public SpeedChangePacket(SpeedState speed)
        {
            Speed = speed;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((int)Speed);
        }

        public void Deserialize(BinaryReader reader)
        {
            Speed = (SpeedState)reader.ReadInt32();
        }

        public void OnDispatched()
        {
            if (SpeedControlScreen.Instance == null)
                return;

            if (Speed == SpeedState.Paused)
            {
                if (!SpeedControlScreen.Instance.IsPaused)
                    SpeedControlScreen.Instance.TogglePause();
            }
            else
            {
                if (SpeedControlScreen.Instance.IsPaused)
                    SpeedControlScreen.Instance.TogglePause();

                SpeedControlScreen.Instance.SetSpeed((int)Speed);
            }

            DebugConsole.Log($"[SpeedChnagePacket] SpeedChangePacket received: Speed set to {Speed}");
        }
    }
}
