using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.SharedStorage;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace ONI_MP.SharedStorage
{
    /// <summary>
    /// Represents an active P2P file transfer
    /// </summary>
    internal class P2PTransfer
    {
        public string TransferID { get; set; }
        public string FileName { get; set; }
        public string FileHash { get; set; }
        public CSteamID RequesterSteamID { get; set; }
        public CSteamID ProviderSteamID { get; set; }
        public int TotalChunks { get; set; }
        public bool[] ReceivedChunks { get; set; }
        public System.DateTime StartTime { get; set; }
        public string LocalPath { get; set; }
        public bool IsDownload { get; set; }
        
        public int ReceivedChunkCount => ReceivedChunks?.Count(c => c) ?? 0;
        public bool IsComplete => ReceivedChunks?.All(c => c) ?? false;
        public float Progress => TotalChunks > 0 ? (float)ReceivedChunkCount / TotalChunks : 0f;
    }

    /// <summary>
    /// Steam P2P based storage provider that uses Steam networking for file transfer
    /// without exposing IP addresses. Files are distributed among lobby members.
    /// </summary>
    public class SteamP2PStorageProvider : ISharedStorageProvider
    {
        private readonly string _localStoragePath;
        private readonly int _chunkSize;
        
        // P2P Transfer Management
        private readonly Dictionary<CSteamID, List<P2PFileManifestPacket.FileInfo>> _peerManifests;
        private readonly Dictionary<string, P2PTransfer> _activeTransfers;
        private readonly Dictionary<string, List<byte[]>> _receivedChunks;
        private readonly object _transferLock = new object();
        
        public bool IsInitialized { get; private set; }
        public string ProviderName => "SteamP2P";
        
        public UnityEvent OnUploadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnUploadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnUploadFailed { get; } = new UnityEvent<Exception>();
        public UnityEvent OnDownloadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnDownloadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnDownloadFailed { get; } = new UnityEvent<Exception>();
        public UnityEvent OnInitializationComplete { get; } = new UnityEvent();
        
        public SteamP2PStorageProvider()
        {
            // Read chunk size from configuration, default to 256KB
            var chunkSizeKB = 256;
            try
            {
                chunkSizeKB = Configuration.GetSteamP2PProperty<int>("ChunkSizeKB");
            }
            catch
            {
                DebugConsole.LogWarning("[SteamP2PStorageProvider] Could not read ChunkSizeKB from config, using default 256KB");
            }
            
            _chunkSize = chunkSizeKB * 1024;
            _localStoragePath = Path.Combine(Application.persistentDataPath, "SteamP2PStorage");
            
            // Initialize P2P management collections
            _peerManifests = new Dictionary<CSteamID, List<P2PFileManifestPacket.FileInfo>>();
            _activeTransfers = new Dictionary<string, P2PTransfer>();
            _receivedChunks = new Dictionary<string, List<byte[]>>();
            
            if (!Directory.Exists(_localStoragePath))
            {
                Directory.CreateDirectory(_localStoragePath);
            }
        }
        
        public void Initialize()
        {
            try
            {
                DebugConsole.Log("[SteamP2PStorageProvider] Starting initialization...");
                
                // Check configuration first
                int chunkSizeKB;
                try
                {
                    chunkSizeKB = Configuration.GetSteamP2PProperty<int>("ChunkSizeKB");
                    DebugConsole.Log($"[SteamP2PStorageProvider] Configuration loaded: ChunkSizeKB = {chunkSizeKB}");
                }
                catch (Exception configEx)
                {
                    DebugConsole.LogWarning($"[SteamP2PStorageProvider] Config read failed: {configEx.Message}, using defaults");
                }
                
                // Check directory
                DebugConsole.Log($"[SteamP2PStorageProvider] Local storage path: {_localStoragePath}");
                if (!Directory.Exists(_localStoragePath))
                {
                    Directory.CreateDirectory(_localStoragePath);
                    DebugConsole.Log("[SteamP2PStorageProvider] Created local storage directory");
                }
                
                if (!SteamManager.Initialized)
                {
                    DebugConsole.LogWarning("[SteamP2PStorageProvider] Steam not initialized yet, will retry when Steam becomes available");
                    // Don't fail immediately - Steam might initialize later
                    IsInitialized = false;
                    OnInitializationComplete?.Invoke();
                    return;
                }
                
                DebugConsole.Log("[SteamP2PStorageProvider] Steam is initialized, proceeding with setup");
                IsInitialized = true;
                DebugConsole.Log("[SteamP2PStorageProvider] Initialized successfully");
                
                // Broadcast our file manifest to other peers
                BroadcastFileManifest();
                
                OnInitializationComplete?.Invoke();
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[SteamP2PStorageProvider] Initialization failed: {ex.Message}");
                DebugConsole.LogError($"[SteamP2PStorageProvider] Stack trace: {ex.StackTrace}");
                IsInitialized = false;
                OnInitializationComplete?.Invoke();
            }
        }
        
        public void UploadFile(string localFilePath, string remoteFileName = null)
        {
            if (!IsInitialized)
            {
                OnUploadFailed?.Invoke(new InvalidOperationException("Provider not initialized"));
                return;
            }
            
            if (!File.Exists(localFilePath))
            {
                OnUploadFailed?.Invoke(new FileNotFoundException($"File not found: {localFilePath}"));
                return;
            }
            
            try
            {
                remoteFileName = remoteFileName ?? Path.GetFileName(localFilePath);
                
                OnUploadStarted?.Invoke();
                DebugConsole.Log($"[SteamP2PStorageProvider] Starting upload of {remoteFileName}");
                
                // Copy to local storage
                var localStorageFile = Path.Combine(_localStoragePath, remoteFileName);
                File.Copy(localFilePath, localStorageFile, true);
                
                DebugConsole.Log($"[SteamP2PStorageProvider] File stored locally for P2P sharing: {remoteFileName}");
                
                // Broadcast updated file manifest to peers
                BroadcastFileManifest();
                
                DebugConsole.Log($"[SteamP2PStorageProvider] Upload completed successfully, invoking callback");
                OnUploadFinished?.Invoke(remoteFileName);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[SteamP2PStorageProvider] Upload failed: {ex.Message}");
                OnUploadFailed?.Invoke(ex);
            }
        }
        
        public void DownloadFile(string remoteFileName, string localFilePath)
        {
            if (!IsInitialized)
            {
                OnDownloadFailed?.Invoke(new InvalidOperationException("Provider not initialized"));
                return;
            }
            
            try
            {
                OnDownloadStarted?.Invoke();
                DebugConsole.Log($"[SteamP2PStorageProvider] Starting download of {remoteFileName}");
                
                // Check if file exists locally first
                var localStorageFile = Path.Combine(_localStoragePath, remoteFileName);
                if (File.Exists(localStorageFile))
                {
                    File.Copy(localStorageFile, localFilePath, true);
                    OnDownloadFinished?.Invoke(localFilePath);
                    DebugConsole.Log($"[SteamP2PStorageProvider] Downloaded from local cache: {remoteFileName}");
                    return;
                }
                
                // Search for file in peer manifests
                var availablePeers = FindPeersWithFile(remoteFileName);
                if (availablePeers.Count == 0)
                {
                    DebugConsole.LogWarning($"[SteamP2PStorageProvider] File not found among peers: {remoteFileName}");
                    OnDownloadFailed?.Invoke(new FileNotFoundException($"File not available from any peer: {remoteFileName}"));
                    return;
                }
                
                // Start P2P download from the first available peer
                var selectedPeer = availablePeers.First();
                var fileInfo = GetFileInfoFromPeer(selectedPeer.Key, remoteFileName);
                if (fileInfo != null)
                {
                    StartP2PDownload(selectedPeer.Key, fileInfo, localFilePath);
                }
                else
                {
                    DebugConsole.LogError($"[SteamP2PStorageProvider] File info not found for {remoteFileName}");
                    OnDownloadFailed?.Invoke(new FileNotFoundException($"File info not available: {remoteFileName}"));
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[SteamP2PStorageProvider] Download failed: {ex.Message}");
                OnDownloadFailed?.Invoke(ex);
            }
        }
        
        public string GetQuotaInfo()
        {
            if (!IsInitialized) return "Provider not initialized";
            
            var localFiles = Directory.GetFiles(_localStoragePath);
            var totalSize = localFiles.Sum(f => new FileInfo(f).Length);
            var fileCount = localFiles.Length;
            
            return $"Steam P2P Storage: {fileCount} files, {FormatBytes(totalSize)} used locally";
        }
        
        public string[] ListFiles()
        {
            if (!IsInitialized) return new string[0];
            
            try
            {
                var allFiles = new HashSet<string>();
                
                // Get local files
                var localFiles = Directory.GetFiles(_localStoragePath)
                    .Select(Path.GetFileName);
                foreach (var file in localFiles)
                {
                    allFiles.Add(file);
                }
                
                // Get files from peer manifests
                lock (_transferLock)
                {
                    foreach (var peerManifest in _peerManifests.Values)
                    {
                        foreach (var fileInfo in peerManifest)
                        {
                            allFiles.Add(fileInfo.FileName);
                        }
                    }
                }
                
                DebugConsole.Log($"[SteamP2PStorageProvider] Found {allFiles.Count} total files (local + peers)");
                return allFiles.ToArray();
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[SteamP2PStorageProvider] Failed to list files: {ex.Message}");
                return new string[0];
            }
        }
        
        #region P2P File Transfer Implementation
        
        /// <summary>
        /// Broadcasts this peer's file manifest to all connected peers
        /// </summary>
        public void BroadcastFileManifest()
        {
            if (!IsInitialized || !MultiplayerSession.InSession)
                return;
                
            try
            {
                var localFiles = Directory.GetFiles(_localStoragePath);
                var fileInfos = new List<P2PFileManifestPacket.FileInfo>();
                
                foreach (var filePath in localFiles)
                {
                    var fileName = Path.GetFileName(filePath);
                    var fileInfo = new System.IO.FileInfo(filePath);
                    var hash = CalculateFileHash(filePath);
                    
                    fileInfos.Add(new P2PFileManifestPacket.FileInfo(
                        fileName, hash, fileInfo.Length, fileInfo.LastWriteTime));
                }
                
                var manifestPacket = new P2PFileManifestPacket(MultiplayerSession.LocalSteamID, fileInfos);
                PacketSender.SendToAllClients(manifestPacket);
                
                DebugConsole.Log($"[SteamP2PStorageProvider] Broadcasted manifest with {fileInfos.Count} files");
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[SteamP2PStorageProvider] Failed to broadcast manifest: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates the manifest for a specific peer
        /// </summary>
        public void UpdatePeerManifest(CSteamID peerSteamID, List<P2PFileManifestPacket.FileInfo> fileInfos)
        {
            lock (_transferLock)
            {
                _peerManifests[peerSteamID] = fileInfos;
            }
            DebugConsole.Log($"[SteamP2PStorageProvider] Updated manifest for peer {peerSteamID}: {fileInfos.Count} files");
        }
        
        /// <summary>
        /// Finds peers that have the specified file
        /// </summary>
        private Dictionary<CSteamID, P2PFileManifestPacket.FileInfo> FindPeersWithFile(string fileName)
        {
            var availablePeers = new Dictionary<CSteamID, P2PFileManifestPacket.FileInfo>();
            
            lock (_transferLock)
            {
                foreach (var peerManifest in _peerManifests)
                {
                    var fileInfo = peerManifest.Value.FirstOrDefault(f => f.FileName == fileName);
                    if (fileInfo != null)
                    {
                        availablePeers[peerManifest.Key] = fileInfo;
                    }
                }
            }
            
            return availablePeers;
        }
        
        /// <summary>
        /// Gets file info from a specific peer
        /// </summary>
        private P2PFileManifestPacket.FileInfo GetFileInfoFromPeer(CSteamID peerSteamID, string fileName)
        {
            lock (_transferLock)
            {
                if (_peerManifests.TryGetValue(peerSteamID, out var manifest))
                {
                    return manifest.FirstOrDefault(f => f.FileName == fileName);
                }
            }
            return null;
        }
        
        /// <summary>
        /// Starts a P2P download from a peer
        /// </summary>
        private void StartP2PDownload(CSteamID peerSteamID, P2PFileManifestPacket.FileInfo fileInfo, string localFilePath)
        {
            var transferID = Guid.NewGuid().ToString();
            var totalChunks = (int)Math.Ceiling((double)fileInfo.FileSize / _chunkSize);
            
            var transfer = new P2PTransfer
            {
                TransferID = transferID,
                FileName = fileInfo.FileName,
                FileHash = fileInfo.FileHash,
                RequesterSteamID = MultiplayerSession.LocalSteamID,
                ProviderSteamID = peerSteamID,
                TotalChunks = totalChunks,
                ReceivedChunks = new bool[totalChunks],
                StartTime = System.DateTime.Now,
                LocalPath = localFilePath,
                IsDownload = true
            };
            
            lock (_transferLock)
            {
                _activeTransfers[transferID] = transfer;
                _receivedChunks[transferID] = new List<byte[]>(new byte[totalChunks][]);
            }
            
            // Request all chunks from the peer
            var chunkIndices = Enumerable.Range(0, totalChunks).ToList();
            var chunkRequestPacket = new P2PChunkRequestPacket(
                MultiplayerSession.LocalSteamID, fileInfo.FileName, fileInfo.FileHash, chunkIndices, transferID);
            
            PacketSender.SendToPlayer(peerSteamID, chunkRequestPacket);
            
            DebugConsole.Log($"[SteamP2PStorageProvider] Started P2P download of {fileInfo.FileName} from {peerSteamID} ({totalChunks} chunks)");
        }
        
        /// <summary>
        /// Handles file requests from other peers
        /// </summary>
        public void HandleFileRequest(CSteamID requesterSteamID, string fileName, string fileHash)
        {
            var localFilePath = Path.Combine(_localStoragePath, fileName);
            if (!File.Exists(localFilePath))
            {
                DebugConsole.Log($"[SteamP2PStorageProvider] File request for {fileName} - file not available locally");
                return;
            }
            
            // Verify hash matches
            var actualHash = CalculateFileHash(localFilePath);
            if (actualHash != fileHash)
            {
                DebugConsole.LogWarning($"[SteamP2PStorageProvider] File request for {fileName} - hash mismatch");
                return;
            }
            
            DebugConsole.Log($"[SteamP2PStorageProvider] Responding to file request for {fileName} from {requesterSteamID}");
        }
        
        /// <summary>
        /// Handles chunk requests from peers
        /// </summary>
        public void HandleChunkRequest(P2PChunkRequestPacket packet)
        {
            var localFilePath = Path.Combine(_localStoragePath, packet.FileName);
            if (!File.Exists(localFilePath))
            {
                DebugConsole.LogWarning($"[SteamP2PStorageProvider] Chunk request for non-existent file: {packet.FileName}");
                return;
            }
            
            DebugConsole.Log($"[SteamP2PStorageProvider] Processing chunk request: {packet.RequestedChunks.Count} chunks of {packet.FileName}");
            
            // Send requested chunks
            if (Game.Instance != null)
            {
                Game.Instance.StartCoroutine(SendChunksAsync(packet));
            }
        }
        
        /// <summary>
        /// Handles received file chunks
        /// </summary>
        public void HandleChunkReceived(P2PFileChunkPacket packet)
        {
            lock (_transferLock)
            {
                if (!_activeTransfers.TryGetValue(packet.TransferID, out var transfer))
                {
                    DebugConsole.LogWarning($"[SteamP2PStorageProvider] Received chunk for unknown transfer: {packet.TransferID}");
                    return;
                }
                
                if (!_receivedChunks.TryGetValue(packet.TransferID, out var chunks))
                {
                    DebugConsole.LogError($"[SteamP2PStorageProvider] Missing chunk storage for transfer: {packet.TransferID}");
                    return;
                }
                
                // Store the chunk
                if (packet.ChunkIndex < chunks.Count)
                {
                    chunks[packet.ChunkIndex] = packet.ChunkData;
                    transfer.ReceivedChunks[packet.ChunkIndex] = true;
                    
                    DebugConsole.Log($"[SteamP2PStorageProvider] Received chunk {packet.ChunkIndex + 1}/{transfer.TotalChunks} for {transfer.FileName} ({transfer.Progress:P1})");
                    
                    // Check if transfer is complete
                    if (transfer.IsComplete)
                    {
                        CompleteTransfer(transfer);
                    }
                }
            }
        }
        
        /// <summary>
        /// Handles transfer completion notifications
        /// </summary>
        public void HandleTransferComplete(P2PTransferCompletePacket packet)
        {
            DebugConsole.Log($"[SteamP2PStorageProvider] Transfer completion notification: {packet.FileName} - {(packet.Success ? "Success" : "Failed")}");
            
            if (!packet.Success)
            {
                DebugConsole.LogError($"[SteamP2PStorageProvider] Peer transfer failed: {packet.ErrorMessage}");
            }
        }
        
        /// <summary>
        /// Completes a file transfer by assembling chunks and writing to disk
        /// </summary>
        private void CompleteTransfer(P2PTransfer transfer)
        {
            try
            {
                if (!_receivedChunks.TryGetValue(transfer.TransferID, out var chunks))
                {
                    throw new InvalidOperationException("Chunk data not found");
                }
                
                // Assemble file from chunks
                using (var fileStream = new FileStream(transfer.LocalPath, FileMode.Create, FileAccess.Write))
                {
                    foreach (var chunk in chunks)
                    {
                        if (chunk != null)
                        {
                            fileStream.Write(chunk, 0, chunk.Length);
                        }
                    }
                }
                
                // Verify file hash
                var actualHash = CalculateFileHash(transfer.LocalPath);
                if (actualHash != transfer.FileHash)
                {
                    File.Delete(transfer.LocalPath);
                    throw new InvalidDataException("File hash verification failed");
                }
                
                // Also store in local cache
                var cacheFilePath = Path.Combine(_localStoragePath, transfer.FileName);
                File.Copy(transfer.LocalPath, cacheFilePath, true);
                
                // Clean up transfer data
                _activeTransfers.Remove(transfer.TransferID);
                _receivedChunks.Remove(transfer.TransferID);
                
                var duration = System.DateTime.Now - transfer.StartTime;
                DebugConsole.Log($"[SteamP2PStorageProvider] Transfer completed: {transfer.FileName} in {duration.TotalSeconds:F1}s");
                
                OnDownloadFinished?.Invoke(transfer.LocalPath);
                
                // If this is a save file and we're a client, automatically load it
                if (!MultiplayerSession.IsHost && (transfer.FileName.StartsWith("ONI_MP_Save_") || transfer.FileName.EndsWith(".sav")))
                {
                    DebugConsole.Log($"[SteamP2PStorageProvider] Auto-loading downloaded save file: {transfer.FileName}");
                    try
                    {
                        SaveHelper.LoadDownloadedSave(transfer.FileName);
                    }
                    catch (System.Exception ex)
                    {
                        DebugConsole.LogError($"[SteamP2PStorageProvider] Failed to load save file: {ex.Message}");
                    }
                }
                
                // Send completion notification to provider
                var completePacket = new P2PTransferCompletePacket(
                    MultiplayerSession.LocalSteamID, transfer.FileName, transfer.FileHash, 
                    transfer.TransferID, true);
                PacketSender.SendToPlayer(transfer.ProviderSteamID, completePacket);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[SteamP2PStorageProvider] Failed to complete transfer: {ex.Message}");
                
                // Clean up
                _activeTransfers.Remove(transfer.TransferID);
                _receivedChunks.Remove(transfer.TransferID);
                
                OnDownloadFailed?.Invoke(ex);
                
                // Send failure notification
                var failurePacket = new P2PTransferCompletePacket(
                    MultiplayerSession.LocalSteamID, transfer.FileName, transfer.FileHash, 
                    transfer.TransferID, false, ex.Message);
                PacketSender.SendToPlayer(transfer.ProviderSteamID, failurePacket);
            }
        }
        
        /// <summary>
        /// Sends file chunks to a requesting peer
        /// </summary>
        private IEnumerator SendChunksAsync(P2PChunkRequestPacket request)
        {
            var localFilePath = Path.Combine(_localStoragePath, request.FileName);
            
            if (!File.Exists(localFilePath))
            {
                DebugConsole.LogError($"[SteamP2PStorageProvider] Cannot send chunks for non-existent file: {request.FileName}");
                yield break;
            }
            
            DebugConsole.Log($"[SteamP2PStorageProvider] Starting to send {request.RequestedChunks.Count} chunks for {request.FileName}");
            
            int sentCount = 0;
            using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                // Calculate the total number of chunks for the entire file
                var totalChunks = (int)Math.Ceiling((double)fileStream.Length / _chunkSize);
                
                DebugConsole.Log($"[SteamP2PStorageProvider] File size: {fileStream.Length} bytes, Chunk size: {_chunkSize}, Total chunks: {totalChunks}");
                
                foreach (var chunkIndex in request.RequestedChunks)
                {
                    bool chunkSent = false;
                    try
                    {
                        var chunkData = new byte[_chunkSize];
                        fileStream.Seek(chunkIndex * _chunkSize, SeekOrigin.Begin);
                        var bytesRead = fileStream.Read(chunkData, 0, _chunkSize);
                        
                        // Resize chunk if it's the last chunk and smaller than chunk size
                        if (bytesRead < _chunkSize)
                        {
                            Array.Resize(ref chunkData, bytesRead);
                        }
                        
                        var chunkPacket = new P2PFileChunkPacket(
                            MultiplayerSession.LocalSteamID, request.FileName, request.FileHash,
                            chunkIndex, totalChunks, chunkData, request.TransferID);
                        
                        PacketSender.SendToPlayer(request.RequesterSteamID, chunkPacket);
                        chunkSent = true;
                        sentCount++;
                        
                        DebugConsole.Log($"[SteamP2PStorageProvider] Sent chunk {chunkIndex + 1}/{totalChunks} ({sentCount}/{request.RequestedChunks.Count} requested) - {bytesRead} bytes");
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogError($"[SteamP2PStorageProvider] Failed to send chunk {chunkIndex}: {ex.Message}");
                        
                        var failurePacket = new P2PTransferCompletePacket(
                            MultiplayerSession.LocalSteamID, request.FileName, request.FileHash, 
                            request.TransferID, false, ex.Message);
                        PacketSender.SendToPlayer(request.RequesterSteamID, failurePacket);
                        yield break;
                    }
                    
                    if (chunkSent)
                    {
                        // Small delay to avoid overwhelming the network
                        yield return new WaitForSeconds(0.05f);
                    }
                }
            }
            
            DebugConsole.Log($"[SteamP2PStorageProvider] Completed sending {sentCount} chunks to {request.RequesterSteamID}");
        }
        
        /// <summary>
        /// Calculates MD5 hash of a file
        /// </summary>
        private string CalculateFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        
        #endregion
        
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
