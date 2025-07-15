using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.Architecture
{
    public interface IPacket
    {
        PacketType Type { get; }

        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);

        void OnDispatched();

    }

}
