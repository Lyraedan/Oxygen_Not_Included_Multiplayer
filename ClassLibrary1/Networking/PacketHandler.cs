﻿using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;

namespace ONI_MP.Networking
{

    public static class PacketHandler
    {
        public static void HandleIncoming(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(ms))
                {
                    PacketType type = (PacketType)reader.ReadByte();
                    var packet = PacketRegistry.Create(type);
                    packet.Deserialize(reader);
                    Dispatch(packet);
                }
            }
        }

        private static void Dispatch(IPacket packet)
        {
            packet.OnDispatched();
        }
    }

}
