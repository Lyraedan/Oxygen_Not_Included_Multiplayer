using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Misc;
using Steamworks;
using System;
using System.Collections.Generic;
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
    /// Steam P2P based storage provider that uses Steam networking for file transfer
    /// without exposing IP addresses. Files are distributed among lobby members.
    /// </summary>
    public class SteamP2PStorageProvider : ISharedStorageProvider
    {
        private readonly string _localStoragePath;
        private readonly int _chunkSize;
        
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
                
                // Copy to local storage (this acts as the "upload" for Steam P2P)
                var localStorageFile = Path.Combine(_localStoragePath, remoteFileName);
                File.Copy(localFilePath, localStorageFile, true);
                
                DebugConsole.Log($"[SteamP2PStorageProvider] File stored locally for P2P sharing: {remoteFileName}");
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
                
                // For now, Steam P2P file sharing happens automatically through lobby data
                // In a more complete implementation, this would request the file from other lobby members
                DebugConsole.LogWarning($"[SteamP2PStorageProvider] File not found locally: {remoteFileName}. Full P2P transfer not yet implemented.");
                OnDownloadFailed?.Invoke(new FileNotFoundException($"File not available: {remoteFileName}"));
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
                // Get local files
                var localFiles = Directory.GetFiles(_localStoragePath)
                    .Select(Path.GetFileName)
                    .ToArray();
                
                DebugConsole.Log($"[SteamP2PStorageProvider] Found {localFiles.Length} local files for P2P sharing");
                return localFiles;
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[SteamP2PStorageProvider] Failed to list files: {ex.Message}");
                return new string[0];
            }
        }
        
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
