using System.IO;
using Steamworks;
using ONI_MP.DebugTools;
using Utils = ONI_MP.Misc.Utils;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.World
{
    public class WorldDataRequestPacket : IPacket
    {
        public CSteamID SenderId;

        public PacketType Type => PacketType.WorldDataRequest;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (!MultiplayerSession.IsHost)
                return;

            // Immediately send full world data back to the requester
            SendWorldData(SenderId);
        }

        private void SendWorldData(CSteamID target)
        {
            DebugConsole.Log($"[WorldDataRequestPacket] Sending world data to {target}");

            var chunks = Utils.CollectChunks(
                startX: 0,
                startY: 0,
                chunkSize: 32,
                numChunksX: Grid.WidthInCells / 32,
                numChunksY: Grid.HeightInCells / 32
            );

            var packet = new WorldDataPacket { Chunks = chunks };
            PacketSender.SendToPlayer(target, packet);

            DebugConsole.Log($"[WorldDataRequestPacket] WorldDataPacket sent to {target}");
        }
    }
}
