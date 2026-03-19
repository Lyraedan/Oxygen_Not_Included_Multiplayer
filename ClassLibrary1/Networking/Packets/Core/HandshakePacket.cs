#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Tests
{
    /// <summary>
    /// Used mainly by LAN to tell the server they're here
    /// </summary>
    public class HandshakePacket : IPacket
    {

        public ulong ClientID;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ClientID);
        }

        public void Deserialize(BinaryReader reader)
        {
            ClientID = reader.ReadUInt64();
        }

        public void OnDispatched()
        {
            //DebugConsole.Log($"[HandshakePacket] Recieved handshake packet from: {ClientID}");
        }
    }
}
#endif