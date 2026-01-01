using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
    /// <summary>
    /// Client sends ACK to confirm that it received a specific chunk
    /// Server uses this to detect lost chunks and resend only the necessary ones
    /// </summary>
    public class ChunkAckPacket : IPacket
    {
        public int SequenceNumber;       // ID of chunk that was received (0, 1, 2, 3...)
        public string TransferId;        // Transfer ID (same as SecureTransferPacket)
        public CSteamID ClientSteamID;   // Who is sending the ACK

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SequenceNumber);
            writer.Write(TransferId);
            writer.Write(ClientSteamID.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            SequenceNumber = reader.ReadInt32();
            TransferId = reader.ReadString();
            ClientSteamID = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            // Only server processes ACKs
            if (!MultiplayerSession.IsHost)
                return;

            DebugConsole.Log($"[ChunkAck] Received ACK {SequenceNumber} from {ClientSteamID} for transfer {TransferId}");

            // Inform transfer system about the ACK
            SaveFileTransferManager.HandleChunkAck(ClientSteamID, TransferId, SequenceNumber);
        }
    }
}