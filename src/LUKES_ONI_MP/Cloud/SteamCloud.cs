using ONI_MP.DebugTools;
using Steamworks;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace ONI_MP.Cloud
{
    /// <summary>
    /// Steam Cloud API integration for save file synchronization.
    /// Provides an alternative to Google Drive with seamless Steam integration.
    /// </summary>
    public class SteamCloud
    {
        private static SteamCloud _instance;
        private bool _initialized = false;

        public bool IsInitialized => _initialized && SteamManager.Initialized;

        private SteamCloud() { }

        public static SteamCloud Instance => _instance ?? (_instance = new SteamCloud());

        public SteamCloudUploader Uploader { get; private set; }
        public SteamCloudDownloader Downloader { get; private set; }
        public UnityEvent OnInitialized { get; } = new UnityEvent();

        /// <summary>
        /// Initializes the Steam Cloud service.
        /// No external credentials required - uses Steam's authentication.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                DebugConsole.Log("SteamCloud already initialized.");
                return;
            }

            if (!SteamManager.Initialized)
            {
                DebugConsole.LogError("SteamCloud: Steam is not initialized!", false);
                return;
            }

            try
            {
                // Check if Steam Cloud is enabled for this user
                if (!SteamRemoteStorage.IsCloudEnabledForAccount())
                {
                    DebugConsole.LogWarning("SteamCloud: Steam Cloud is disabled for this account!", false);
                    return;
                }

                // Check if Steam Cloud is enabled for this app
                if (!SteamRemoteStorage.IsCloudEnabledForApp())
                {
                    DebugConsole.LogWarning("SteamCloud: Steam Cloud is disabled for this application!", false);
                    return;
                }

                Uploader = new SteamCloudUploader();
                Downloader = new SteamCloudDownloader();

                _initialized = true;
                DebugConsole.Log("SteamCloud: Initialized successfully.");
                OnInitialized.Invoke();
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"SteamCloud: Initialization failed: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Gets quota information for Steam Cloud storage.
        /// </summary>
        public (ulong totalBytes, ulong availableBytes) GetQuota()
        {
            if (!IsInitialized)
                return (0, 0);

            if (SteamRemoteStorage.GetQuota(out ulong totalBytes, out ulong availableBytes))
            {
                return (totalBytes, availableBytes);
            }

            return (0, 0);
        }

        /// <summary>
        /// Gets the number of files stored in Steam Cloud.
        /// </summary>
        public int GetFileCount()
        {
            if (!IsInitialized)
                return 0;

            return SteamRemoteStorage.GetFileCount();
        }

        /// <summary>
        /// Lists all files in Steam Cloud.
        /// </summary>
        public string[] GetFileList()
        {
            if (!IsInitialized)
                return new string[0];

            int fileCount = GetFileCount();
            string[] files = new string[fileCount];

            for (int i = 0; i < fileCount; i++)
            {
                string fileName = SteamRemoteStorage.GetFileNameAndSize(i, out int fileSize);
                files[i] = fileName;
            }

            return files;
        }

        /// <summary>
        /// Checks if a file exists in Steam Cloud.
        /// </summary>
        public bool FileExists(string fileName)
        {
            if (!IsInitialized)
                return false;

            return SteamRemoteStorage.FileExists(fileName);
        }

        /// <summary>
        /// Gets the size of a file in Steam Cloud.
        /// </summary>
        public int GetFileSize(string fileName)
        {
            if (!IsInitialized)
                return 0;

            return SteamRemoteStorage.GetFileSize(fileName);
        }

        /// <summary>
        /// Gets the timestamp of when a file was last modified.
        /// </summary>
        public DateTime GetFileTimestamp(string fileName)
        {
            if (!IsInitialized)
                return DateTime.MinValue;

            uint timestamp = SteamRemoteStorage.GetFileTimestamp(fileName);
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }
    }
}
