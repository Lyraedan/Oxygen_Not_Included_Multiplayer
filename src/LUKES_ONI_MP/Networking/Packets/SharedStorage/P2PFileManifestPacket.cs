using System.IO;
using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.SharedStorage;
using Steamworks;

namespace ONI_MP.Networking.Packets.SharedStorage
{
    /// <summary>
    /// Packet announcing available files to peers
    /// </summary>
    public class P2PFileManifestPacket : IPacket
    {
        public PacketType Type => PacketType.P2PFileManifest;
        
        public CSteamID SenderSteamID;
        public List<FileInfo> AvailableFiles;
        
        public class FileInfo
        {
            public string FileName;
            public string FileHash;
            public long FileSize;
            public System.DateTime LastModified;
            
            public FileInfo() { }
            
            public FileInfo(string fileName, string fileHash, long fileSize, System.DateTime lastModified)
            {
                FileName = fileName;
                FileHash = fileHash;
                FileSize = fileSize;
                LastModified = lastModified;
            }
        }
        
        public P2PFileManifestPacket() 
        { 
            AvailableFiles = new List<FileInfo>();
        }
        
        public P2PFileManifestPacket(CSteamID senderSteamID, List<FileInfo> availableFiles)
        {
            SenderSteamID = senderSteamID;
            AvailableFiles = availableFiles ?? new List<FileInfo>();
        }
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderSteamID.m_SteamID);
            writer.Write(AvailableFiles.Count);
            
            foreach (var file in AvailableFiles)
            {
                writer.Write(file.FileName ?? "");
                writer.Write(file.FileHash ?? "");
                writer.Write(file.FileSize);
                writer.Write(file.LastModified.ToBinary());
            }
        }
        
        public void Deserialize(BinaryReader reader)
        {
            SenderSteamID = new CSteamID(reader.ReadUInt64());
            int fileCount = reader.ReadInt32();
            
            AvailableFiles = new List<FileInfo>();
            for (int i = 0; i < fileCount; i++)
            {
                var fileInfo = new FileInfo
                {
                    FileName = reader.ReadString(),
                    FileHash = reader.ReadString(),
                    FileSize = reader.ReadInt64(),
                    LastModified = System.DateTime.FromBinary(reader.ReadInt64())
                };
                AvailableFiles.Add(fileInfo);
            }
        }
        
        public void OnDispatched()
        {
            // Only process if it's not from ourselves
            if (SenderSteamID == MultiplayerSession.LocalSteamID)
                return;
                
            DebugConsole.Log($"[P2PFileManifestPacket] Received manifest from {SenderSteamID} with {AvailableFiles.Count} files");
            
            // Update our peer file registry
            var steamP2PProvider = SharedStorageManager.Instance.CurrentProviderInstance as SteamP2PStorageProvider;
            if (steamP2PProvider != null)
            {
                steamP2PProvider.UpdatePeerManifest(SenderSteamID, AvailableFiles);
            }
        }
    }
}
