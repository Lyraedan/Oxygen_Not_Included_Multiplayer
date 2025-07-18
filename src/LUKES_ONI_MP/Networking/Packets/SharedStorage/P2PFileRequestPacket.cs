using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.SharedStorage;
using Steamworks;

namespace ONI_MP.Networking.Packets.SharedStorage
{
    /// <summary>
    /// Packet to request a file from peers in the Steam P2P network
    /// </summary>
    public class P2PFileRequestPacket : IPacket
    {
        public PacketType Type => PacketType.P2PFileRequest;
        
        public CSteamID RequesterSteamID;
        public string FileName;
        public string FileHash; // MD5 hash for validation
        
        public P2PFileRequestPacket() { }
        
        public P2PFileRequestPacket(CSteamID requesterSteamID, string fileName, string fileHash)
        {
            RequesterSteamID = requesterSteamID;
            FileName = fileName;
            FileHash = fileHash;
        }
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(RequesterSteamID.m_SteamID);
            writer.Write(FileName ?? "");
            writer.Write(FileHash ?? "");
        }
        
        public void Deserialize(BinaryReader reader)
        {
            RequesterSteamID = new CSteamID(reader.ReadUInt64());
            FileName = reader.ReadString();
            FileHash = reader.ReadString();
        }
        
        public void OnDispatched()
        {
            // Only respond if we're not the requester and we have the file
            if (RequesterSteamID == MultiplayerSession.LocalSteamID)
                return;
                
            DebugConsole.Log($"[P2PFileRequestPacket] Received request for {FileName} from {RequesterSteamID}");
            
            // Check if we have this file and respond
            var provider = SharedStorageManager.Instance.CurrentProviderInstance as SteamP2PStorageProvider;
            if (provider != null)
            {
                provider.HandleFileRequest(RequesterSteamID, FileName, FileHash);
            }
        }
    }
}
