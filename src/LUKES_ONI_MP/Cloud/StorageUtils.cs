using ONI_MP.DebugTools;
using ONI_MP.Misc.World;
using ONI_MP.Networking.Packets.Cloud;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking;
using Steamworks;
using System;
using System.IO;

namespace ONI_MP.Cloud
{
    /// <summary>
    /// Universal storage utilities that work with any configured shared access storage provider.
    /// Replaces legacy cloud storage with a unified interface.
    /// </summary>
    public class StorageUtils
    {
        /// <summary>
        /// Uploads the current save file and sends sharing information to all clients.
        /// </summary>
        public static void UploadAndSendToAllClients()
        {
            if (!SharedAccessStorageManager.Instance.IsInitialized)
            {
                DebugConsole.LogError("StorageUtils: Shared access storage not initialized!", false);
                return;
            }

            SharedAccessStorageManager.Instance.OnUploadFinished.RemoveAllListeners();
            SharedAccessStorageManager.Instance.OnUploadFinished.AddListener((result) =>
            {
                string originalFileName = Path.GetFileName(SaveLoader.GetActiveSaveFilePath());

                // Send appropriate packet based on current provider
                if (SharedAccessStorageManager.Instance.CurrentProvider == "GoogleDrive")
                {
                    var packet = new GoogleDriveFileSharePacket
                    {
                        FileName = originalFileName,
                        ShareLink = result
                    };
                    PacketSender.SendToAllClients(packet);
                }
                else if (SharedAccessStorageManager.Instance.CurrentProvider == "HttpSharedStorage")
                {
                    var packet = new HttpCloudFileSharePacket
                    {
                        FileName = originalFileName,
                        CloudFileName = result,
                        ServerUrl = Configuration.GetHttpCloudProperty<string>("HttpServerUrl"),
                        SessionId = Configuration.GetHttpCloudProperty<string>("SessionId"),
                        FileSize = (int)new FileInfo(SaveLoader.GetActiveSaveFilePath()).Length,
                        Timestamp = System.DateTime.UtcNow
                    };
                    PacketSender.SendToAllClients(packet);
                }

                if (GameServerHardSync.IsHardSyncInProgress)
                {
                    GameServerHardSync.IsHardSyncInProgress = false;
                }
            });

            SharedAccessStorageManager.Instance.OnUploadFailed.RemoveAllListeners();
            SharedAccessStorageManager.Instance.OnUploadFailed.AddListener((ex) =>
            {
                DebugConsole.LogError($"StorageUtils: Upload failed: {ex.Message}", false);
            });

            // Upload current save file
            UploadSaveFile();
        }

        /// <summary>
        /// Uploads the current save file and sends sharing information to a specific client.
        /// </summary>
        public static void UploadAndSendToClient(CSteamID requester)
        {
            if (!SharedAccessStorageManager.Instance.IsInitialized)
            {
                DebugConsole.LogError("StorageUtils: Shared access storage not initialized!", false);
                return;
            }

            SharedAccessStorageManager.Instance.OnUploadFinished.RemoveAllListeners();
            SharedAccessStorageManager.Instance.OnUploadFinished.AddListener((result) =>
            {
                string originalFileName = Path.GetFileName(SaveLoader.GetActiveSaveFilePath());

                // Send appropriate packet based on current provider
                if (SharedAccessStorageManager.Instance.CurrentProvider == "GoogleDrive")
                {
                    var packet = new GoogleDriveFileSharePacket
                    {
                        FileName = originalFileName,
                        ShareLink = result
                    };
                    PacketSender.SendToPlayer(requester, packet);
                }
                else if (SharedAccessStorageManager.Instance.CurrentProvider == "HttpSharedStorage")
                {
                    var packet = new HttpCloudFileSharePacket
                    {
                        FileName = originalFileName,
                        CloudFileName = result,
                        ServerUrl = Configuration.GetHttpCloudProperty<string>("HttpServerUrl"),
                        SessionId = Configuration.GetHttpCloudProperty<string>("SessionId"),
                        FileSize = (int)new FileInfo(SaveLoader.GetActiveSaveFilePath()).Length,
                        Timestamp = System.DateTime.UtcNow
                    };
                    PacketSender.SendToPlayer(requester, packet);
                }
            });

            UploadSaveFile();
        }

        /// <summary>
        /// Uploads the current save file to the configured shared access storage.
        /// </summary>
        public static void UploadSaveFile()
        {
            if (!SharedAccessStorageManager.Instance.IsInitialized)
            {
                DebugConsole.LogError("StorageUtils: Shared access storage not initialized!", false);
                return;
            }

            try
            {
                var path = SaveLoader.GetActiveSaveFilePath();
                SaveLoader.Instance.Save(path); // Saves current state to that file

                string originalFileName = Path.GetFileName(path);
                string remoteFileName = $"ONI_MP_Save_{System.DateTime.UtcNow:yyyyMMdd_HHmmss}_{originalFileName}";

                DebugConsole.Log($"StorageUtils: Uploading save file {originalFileName} as {remoteFileName}");
                SharedAccessStorageManager.Instance.UploadFile(path, remoteFileName);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageUtils: Failed to upload save file: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Downloads a save file from shared access storage and loads it.
        /// </summary>
        public static void DownloadAndLoadSaveFile(string remoteFileName, string localFileName)
        {
            if (!SharedAccessStorageManager.Instance.IsInitialized)
            {
                DebugConsole.LogError("StorageUtils: Shared access storage not initialized!", false);
                return;
            }

            try
            {
                // Determine local save path
                string saveDir = Path.GetDirectoryName(SaveLoader.GetActiveSaveFilePath());
                string localPath = Path.Combine(saveDir, localFileName);

                DebugConsole.Log($"StorageUtils: Downloading {remoteFileName} to {localPath}");

                SharedAccessStorageManager.Instance.OnDownloadFinished.RemoveAllListeners();
                SharedAccessStorageManager.Instance.OnDownloadFinished.AddListener((downloadPath) =>
                {
                    try
                    {
                        DebugConsole.Log($"StorageUtils: Download complete, loading {localFileName}");
                        SaveLoader.Instance.Load(downloadPath);
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogError($"StorageUtils: Failed to load downloaded save: {ex.Message}", false);
                    }
                });

                SharedAccessStorageManager.Instance.OnDownloadFailed.RemoveAllListeners();
                SharedAccessStorageManager.Instance.OnDownloadFailed.AddListener((exception) =>
                {
                    DebugConsole.LogError($"StorageUtils: Download failed: {exception.Message}", false);
                });

                SharedAccessStorageManager.Instance.DownloadFile(remoteFileName, localPath);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageUtils: Failed to download save file: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Gets information about the current shared access storage provider.
        /// </summary>
        public static string GetProviderInfo()
        {
            if (!SharedAccessStorageManager.Instance.IsInitialized)
                return "Shared access storage not initialized";

            string provider = SharedAccessStorageManager.Instance.CurrentProvider;
            string quota = SharedAccessStorageManager.Instance.GetQuotaInfo();
            
            return $"Provider: {provider} | {quota}";
        }

        /// <summary>
        /// Lists available save files in shared access storage.
        /// </summary>
        public static string[] GetAvailableSaveFiles()
        {
            if (!SharedAccessStorageManager.Instance.IsInitialized)
                return new string[0];

            return SharedAccessStorageManager.Instance.ListFiles();
        }

        /// <summary>
        /// Switches to a different shared access storage provider.
        /// </summary>
        public static void SwitchProvider(string providerName)
        {
            if (!SharedAccessStorageManager.Instance.IsInitialized)
            {
                DebugConsole.LogError("StorageUtils: Shared access storage not initialized!", false);
                return;
            }

            SharedAccessStorageManager.Instance.SwitchProvider(providerName);
        }

        /// <summary>
        /// Checks if the current provider supports a specific feature.
        /// </summary>
        public static bool SupportsFeature(CloudFeature feature)
        {
            if (!SharedAccessStorageManager.Instance.IsInitialized)
                return false;

            switch (SharedAccessStorageManager.Instance.CurrentProvider)
            {
                case "GoogleDrive":
                    switch (feature)
                    {
                        case CloudFeature.ShareLinks:
                            return true;
                        case CloudFeature.FileQuota:
                        case CloudFeature.FileListing:
                        case CloudFeature.FileTimestamps:
                            return false;
                        default:
                            return false;
                    }

                case "HttpSharedStorage":
                    switch (feature)
                    {
                        case CloudFeature.FileListing:
                        case CloudFeature.FileTimestamps:
                            return true;
                        case CloudFeature.FileQuota:
                        case CloudFeature.ShareLinks:
                            return false;
                        default:
                            return false;
                    }

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Cloud storage features that providers may or may not support.
    /// </summary>
    public enum CloudFeature
    {
        FileQuota,
        FileListing,
        FileTimestamps,
        ShareLinks
    }
}
