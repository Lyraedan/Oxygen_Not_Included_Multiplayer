using System;
using UnityEngine.Events;

namespace ONI_MP.Cloud
{
    /// <summary>
    /// Interface for shared access storage providers.
    /// </summary>
    public interface ISharedAccessStorageProvider
    {
        bool IsInitialized { get; }
        string ProviderName { get; }

        UnityEvent OnUploadStarted { get; }
        UnityEvent<string> OnUploadFinished { get; }
        UnityEvent<Exception> OnUploadFailed { get; }
        UnityEvent OnDownloadStarted { get; }
        UnityEvent<string> OnDownloadFinished { get; }
        UnityEvent<Exception> OnDownloadFailed { get; }

        void Initialize();
        void UploadFile(string localFilePath, string remoteFileName = null);
        void DownloadFile(string remoteFileName, string localFilePath);
        string GetQuotaInfo();
        string[] ListFiles();
    }
}
