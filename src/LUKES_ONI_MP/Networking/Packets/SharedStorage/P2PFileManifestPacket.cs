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
            DebugConsole.Log($"[P2PFileManifestPacket] DEBUG CHECKPOINT 1 - About to access SharedStorageManager");
            
            // Update our peer file registry
            var steamP2PProvider = SharedStorageManager.Instance.CurrentProviderInstance as SteamP2PStorageProvider;
            DebugConsole.Log($"[P2PFileManifestPacket] DEBUG CHECKPOINT 2 - SharedStorageManager access completed");
            DebugConsole.Log($"[P2PFileManifestPacket] SharedStorageManager.Instance: {SharedStorageManager.Instance != null}");
            DebugConsole.Log($"[P2PFileManifestPacket] CurrentProviderInstance: {SharedStorageManager.Instance?.CurrentProviderInstance != null}");
            DebugConsole.Log($"[P2PFileManifestPacket] steamP2PProvider: {steamP2PProvider != null}");
            
            if (steamP2PProvider != null)
            {
                steamP2PProvider.UpdatePeerManifest(SenderSteamID, AvailableFiles);
                DebugConsole.Log($"[P2PFileManifestPacket] DEBUG CHECKPOINT 3 - UpdatePeerManifest completed");
                
                // Debug logging
                DebugConsole.Log($"[P2PFileManifestPacket] IsHost: {MultiplayerSession.IsHost}, SenderIsHost: {SenderSteamID == MultiplayerSession.HostSteamID}");
                DebugConsole.Log($"[P2PFileManifestPacket] HostSteamID: {MultiplayerSession.HostSteamID}");
                
                // If we're a client and this manifest is from the host, automatically download and load save files
                if (!MultiplayerSession.IsHost && SenderSteamID == MultiplayerSession.HostSteamID)
                {
                    DebugConsole.Log($"[P2PFileManifestPacket] Client received manifest from host - checking for save files to auto-download");
                    foreach (var file in AvailableFiles)
                    {
                        DebugConsole.Log($"[P2PFileManifestPacket] Examining file: {file.FileName}");
                        // Look for save files (they should have ONI_MP_Save prefix or .sav extension)
                        if (file.FileName.StartsWith("ONI_MP_Save_") || file.FileName.EndsWith(".sav"))
                        {
                            DebugConsole.Log($"[P2PFileManifestPacket] Found save file, directly downloading: {file.FileName}");
                            
                            // Directly download and load the save file using StorageUtils
                            try 
                            {
                                // Use the original filename for loading (without ONI_MP_Save prefix)
                                string originalFileName = file.FileName.StartsWith("ONI_MP_Save_") ? 
                                    file.FileName.Substring(file.FileName.LastIndexOf('_') + 1) : 
                                    file.FileName;
                                    
                                DebugConsole.Log($"[P2PFileManifestPacket] Triggering download: remote={file.FileName}, local={originalFileName}");
                                StorageUtils.DownloadAndLoadSaveFile(file.FileName, originalFileName);
                                break; // Only download the first save file found
                            }
                            catch (System.Exception ex)
                            {
                                DebugConsole.LogError($"[P2PFileManifestPacket] Failed to download save file: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    DebugConsole.Log($"[P2PFileManifestPacket] Not requesting files - IsHost: {MultiplayerSession.IsHost}, SenderIsHost: {SenderSteamID == MultiplayerSession.HostSteamID}");
                }
            }
            else
            {
                DebugConsole.LogError($"[P2PFileManifestPacket] steamP2PProvider is null - cannot process manifest");
            }
        }
    }
}
