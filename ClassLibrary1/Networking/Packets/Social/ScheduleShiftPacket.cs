using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.Social
{
    public class ScheduleShiftPacket : IPacket
    {
        // Use for shifting groups
        public enum ShiftDirection
        {
            UP, DOWN, LEFT, RIGHT
        }

        public void Serialize(BinaryWriter writer)
        {
        }

        public void Deserialize(BinaryReader reader)
        {
        }

        public void OnDispatched()
        {
        }

    }
}
