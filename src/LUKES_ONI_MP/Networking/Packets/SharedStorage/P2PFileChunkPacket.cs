using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.SharedStorage;
using Steamworks;

namespace ONI_MP.Networking.Packets.SharedStorage
{
    /// <summary>
    /// Packet containing a chunk of file data for P2P transfer
    /// </summary>
    public class P2PFileChunkPacket : IPacket
    {
        public PacketType Type => PacketType.P2PFileChunk;
        
        public CSteamID SenderSteamID;
        public string FileName;
        public string FileHash;
        public int ChunkIndex;
        public int TotalChunks;
        public byte[] ChunkData;
        public int ChunkSize;
        public string TransferID; // Unique ID for this transfer session
        
        public P2PFileChunkPacket() { }
        
        public P2PFileChunkPacket(CSteamID senderSteamID, string fileName, string fileHash, 
            int chunkIndex, int totalChunks, byte[] chunkData, string transferID)
        {
            SenderSteamID = senderSteamID;
            FileName = fileName;
            FileHash = fileHash;
            ChunkIndex = chunkIndex;
            TotalChunks = totalChunks;
            ChunkData = chunkData;
            ChunkSize = chunkData?.Length ?? 0;
            TransferID = transferID;
        }
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderSteamID.m_SteamID);
            writer.Write(FileName ?? "");
            writer.Write(FileHash ?? "");
            writer.Write(ChunkIndex);
            writer.Write(TotalChunks);
            writer.Write(ChunkSize);
            writer.Write(TransferID ?? "");
            
            if (ChunkData != null && ChunkSize > 0)
            {
                writer.Write(ChunkData, 0, ChunkSize);
            }
        }
        
        public void Deserialize(BinaryReader reader)
        {
            SenderSteamID = new CSteamID(reader.ReadUInt64());
            FileName = reader.ReadString();
            FileHash = reader.ReadString();
            ChunkIndex = reader.ReadInt32();
            TotalChunks = reader.ReadInt32();
            ChunkSize = reader.ReadInt32();
            TransferID = reader.ReadString();
            
            if (ChunkSize > 0)
            {
                ChunkData = reader.ReadBytes(ChunkSize);
            }
        }
        
        public void OnDispatched()
        {
            DebugConsole.Log($"[P2PFileChunkPacket] Received chunk {ChunkIndex}/{TotalChunks} for {FileName} from {SenderSteamID}");
            
            // Handle chunk reception in the storage provider
            var steamP2PProvider = SharedStorageManager.Instance.CurrentProviderInstance as SteamP2PStorageProvider;
            if (steamP2PProvider != null)
            {
                steamP2PProvider.HandleChunkReceived(this);
            }
        }
    }
}
