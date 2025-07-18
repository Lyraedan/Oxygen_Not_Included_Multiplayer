using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Architecture
{

    public static class PacketHandler
    {
        public static bool readyToProcess = true;

        public static void HandleIncoming(byte[] data)
        {
            if(!readyToProcess)
            {
                return;
            }

            using (var ms = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(ms))
                {
                    DebugConsole.Log($"[Recv] Type={data[0]}, OnHost={PacketSender.Platform.IsHost}, Frame={Time.frameCount}");

                    PacketType type = (PacketType)reader.ReadByte();
                    var packet = PacketRegistry.Create(type);
                    packet.Deserialize(reader);
                    Dispatch(packet);
                }
            }
        }

        private static void Dispatch(IPacket packet)
        {
            MasterNetworkingComponent.scheduler.Run(() => packet.OnDispatched()); // Run on Unity's Main thread
        }
    }

}
