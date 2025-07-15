using System.IO;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using ONI_MP.Menus;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Packets.Core
{
    public class ClientReadyStatusUpdatePacket : IPacket
    {
        public PacketType Type => PacketType.ClientReadyStatusUpdate;

        public string Message;

        public ClientReadyStatusUpdatePacket() { }

        public ClientReadyStatusUpdatePacket(string message)
        {
            Message = message;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Message);
        }

        public void Deserialize(BinaryReader reader)
        {
            Message = reader.ReadString();
        }

        public void OnDispatched()
        {
            // Host updates theirs on each ready status packet so we dont do anythingh here
            if (MultiplayerSession.IsHost)
                return;

            MultiplayerOverlay.Show(Message);
        }
    }
}
