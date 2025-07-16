using System;
using System.IO;
using UnityEngine.Events;

namespace ONI_MP.Cloud
{
    /// <summary>
    /// Google Drive provider implementation.
    /// </summary>
    public class GoogleDriveProvider : ISharedAccessStorageProvider
    {
        public bool IsInitialized => GoogleDrive.Instance.IsInitialized;
        public string ProviderName => "GoogleDrive";

        public UnityEvent OnUploadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnUploadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnUploadFailed { get; } = new UnityEvent<Exception>();
        public UnityEvent OnDownloadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnDownloadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnDownloadFailed { get; } = new UnityEvent<Exception>();

        public void Initialize()
        {
            GoogleDrive.Instance.Initialize();
            
            if (IsInitialized)
            {
                // Connect Google Drive events to provider events
                GoogleDrive.Instance.Uploader.OnUploadStarted.AddListener(() => OnUploadStarted.Invoke());
                GoogleDrive.Instance.Uploader.OnUploadFinished.AddListener((shareLink) => OnUploadFinished.Invoke(shareLink));
                GoogleDrive.Instance.Uploader.OnUploadFailed.AddListener((ex) => OnUploadFailed.Invoke(ex));
                
                GoogleDrive.Instance.Downloader.OnDownloadStarted.AddListener(() => OnDownloadStarted.Invoke());
                GoogleDrive.Instance.Downloader.OnDownloadFinished.AddListener((filePath) => OnDownloadFinished.Invoke(filePath));
                GoogleDrive.Instance.Downloader.OnDownloadFailed.AddListener((ex) => OnDownloadFailed.Invoke(ex));
            }
        }

        public void UploadFile(string localFilePath, string remoteFileName = null)
        {
            // Google Drive uploader doesn't use remoteFileName in the same way
            GoogleDrive.Instance.Uploader.UploadFile(localFilePath);
        }

        public void DownloadFile(string remoteFileName, string localFilePath)
        {
            // For Google Drive, remoteFileName is actually a file ID or share link
            GoogleDrive.Instance.Downloader.DownloadFile(remoteFileName, localFilePath);
        }

        public string GetQuotaInfo()
        {
            return "Google Drive quota information not available through current API";
        }

        public string[] ListFiles()
        {
            return new string[0]; // Google Drive doesn't have a simple file listing in current implementation
        }
    }
}
