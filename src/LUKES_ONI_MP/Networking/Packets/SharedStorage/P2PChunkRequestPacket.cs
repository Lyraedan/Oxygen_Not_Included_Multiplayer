using System.IO;
using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.SharedStorage;
using Steamworks;

namespace ONI_MP.Networking.Packets.SharedStorage
{
    /// <summary>
    /// Packet to request specific chunks from a peer
    /// </summary>
    public class P2PChunkRequestPacket : IPacket
    {
        public PacketType Type => PacketType.P2PChunkRequest;
        
        public CSteamID RequesterSteamID;
        public string FileName;
        public string FileHash;
        public List<int> RequestedChunks;
        public string TransferID;
        
        public P2PChunkRequestPacket() 
        { 
            RequestedChunks = new List<int>();
        }
        
        public P2PChunkRequestPacket(CSteamID requesterSteamID, string fileName, string fileHash, 
            List<int> requestedChunks, string transferID)
        {
            RequesterSteamID = requesterSteamID;
            FileName = fileName;
            FileHash = fileHash;
            RequestedChunks = requestedChunks ?? new List<int>();
            TransferID = transferID;
        }
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(RequesterSteamID.m_SteamID);
            writer.Write(FileName ?? "");
            writer.Write(FileHash ?? "");
            writer.Write(TransferID ?? "");
            writer.Write(RequestedChunks.Count);
            
            foreach (int chunkIndex in RequestedChunks)
            {
                writer.Write(chunkIndex);
            }
        }
        
        public void Deserialize(BinaryReader reader)
        {
            RequesterSteamID = new CSteamID(reader.ReadUInt64());
            FileName = reader.ReadString();
            FileHash = reader.ReadString();
            TransferID = reader.ReadString();
            int chunkCount = reader.ReadInt32();
            
            RequestedChunks = new List<int>();
            for (int i = 0; i < chunkCount; i++)
            {
                RequestedChunks.Add(reader.ReadInt32());
            }
        }
        
        public void OnDispatched()
        {
            // Only respond if we're not the requester
            if (RequesterSteamID == MultiplayerSession.LocalSteamID)
                return;
                
            DebugConsole.Log($"[P2PChunkRequestPacket] Received request for {RequestedChunks.Count} chunks of {FileName} from {RequesterSteamID}");
            
            // Handle chunk request in the storage provider
            var steamP2PProvider = SharedStorageManager.Instance.CurrentProviderInstance as SteamP2PStorageProvider;
            if (steamP2PProvider != null)
            {
                steamP2PProvider.HandleChunkRequest(this);
            }
        }
    }
}
