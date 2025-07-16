using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Cloud;
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
            if (!CloudStorageManager.Instance.IsInitialized)
            {
                DebugConsole.LogError("CloudUtils: Cloud storage not initialized!", false);
                return;
            }

            CloudStorageManager.Instance.OnUploadFinished.RemoveAllListeners();
            CloudStorageManager.Instance.OnUploadFinished.AddListener((result) =>
            {
                string originalFileName = Path.GetFileName(SaveLoader.GetActiveSaveFilePath());

                // Send appropriate packet based on current provider
                if (CloudStorageManager.Instance.CurrentProvider == "SteamCloud")
                {
                    var packet = new SteamCloudFileSharePacket
                    {
                        FileName = originalFileName,
                        CloudFileName = result,
                        FileSize = SteamCloud.Instance.GetFileSize(result),
                        Timestamp = SteamCloud.Instance.GetFileTimestamp(result)
                    };
                    PacketSender.SendToAllClients(packet);
                }
                else if (CloudStorageManager.Instance.CurrentProvider == "GoogleDrive")
                {
                    var packet = new GoogleDriveFileSharePacket
                    {
                        FileName = originalFileName,
                        ShareLink = result
                    };
                    PacketSender.SendToAllClients(packet);
                }
                else if (CloudStorageManager.Instance.CurrentProvider == "HttpCloud")
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

            UploadSaveFile();
        }

        /// <summary>
        /// Uploads the current save file and sends sharing information to a specific client.
        /// </summary>
        public static void UploadAndSendToClient(CSteamID requester)
        {
            if (!CloudStorageManager.Instance.IsInitialized)
            {
                DebugConsole.LogError("CloudUtils: Cloud storage not initialized!", false);
                return;
            }

            CloudStorageManager.Instance.OnUploadFinished.RemoveAllListeners();
            CloudStorageManager.Instance.OnUploadFinished.AddListener((result) =>
            {
                string originalFileName = Path.GetFileName(SaveLoader.GetActiveSaveFilePath());

                // Send appropriate packet based on current provider
                if (CloudStorageManager.Instance.CurrentProvider == "SteamCloud")
                {
                    var packet = new SteamCloudFileSharePacket
                    {
                        FileName = originalFileName,
                        CloudFileName = result,
                        FileSize = SteamCloud.Instance.GetFileSize(result),
                        Timestamp = SteamCloud.Instance.GetFileTimestamp(result)
                    };
                    PacketSender.SendToPlayer(requester, packet);
                }
                else if (CloudStorageManager.Instance.CurrentProvider == "GoogleDrive")
                {
                    var packet = new GoogleDriveFileSharePacket
                    {
                        FileName = originalFileName,
                        ShareLink = result
                    };
                    PacketSender.SendToPlayer(requester, packet);
                }
                else if (CloudStorageManager.Instance.CurrentProvider == "HttpCloud")
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
        /// Uploads the current save file to the configured cloud storage.
        /// </summary>
        public static void UploadSaveFile()
        {
            if (!CloudStorageManager.Instance.IsInitialized)
            {
                DebugConsole.LogError("CloudUtils: Cloud storage not initialized!", false);
                return;
            }

            try
            {
                var path = SaveLoader.GetActiveSaveFilePath();
                SaveLoader.Instance.Save(path); // Saves current state to that file

                string originalFileName = Path.GetFileName(path);
                DebugConsole.Log($"CloudUtils: Uploading save file {originalFileName} using {CloudStorageManager.Instance.CurrentProvider}");

                CloudStorageManager.Instance.UploadFile(path);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"CloudUtils: Failed to upload save file: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Gets information about the current cloud storage provider.
        /// </summary>
        public static string GetProviderInfo()
        {
            if (!CloudStorageManager.Instance.IsInitialized)
                return "Cloud storage not initialized";

            string provider = CloudStorageManager.Instance.CurrentProvider;
            string quota = CloudStorageManager.Instance.GetQuotaInfo();
            
            return $"Provider: {provider} | {quota}";
        }

        /// <summary>
        /// Lists available save files in cloud storage.
        /// </summary>
        public static string[] GetAvailableSaveFiles()
        {
            if (!CloudStorageManager.Instance.IsInitialized)
                return new string[0];

            return CloudStorageManager.Instance.ListFiles();
        }

        /// <summary>
        /// Switches to a different cloud storage provider.
        /// </summary>
        public static void SwitchProvider(string providerName)
        {
            DebugConsole.Log($"CloudUtils: Switching to provider: {providerName}");
            CloudStorageManager.Instance.SwitchProvider(providerName);
        }

        /// <summary>
        /// Checks if the current provider supports a specific feature.
        /// </summary>
        public static bool SupportsFeature(CloudFeature feature)
        {
            if (!CloudStorageManager.Instance.IsInitialized)
                return false;

            switch (CloudStorageManager.Instance.CurrentProvider)
            {
                case "SteamCloud":
                    switch (feature)
                    {
                        case CloudFeature.FileQuota:
                        case CloudFeature.FileListing:
                        case CloudFeature.FileTimestamps:
                            return true;
                        case CloudFeature.ShareLinks:
                            return false;
                        default:
                            return false;
                    }

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

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Features that cloud storage providers may support.
    /// </summary>
    public enum CloudFeature
    {
        FileQuota,
        FileListing,
        FileTimestamps,
        ShareLinks
    }
}
