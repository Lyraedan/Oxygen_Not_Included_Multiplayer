using ONI_MP.DebugTools;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace ONI_MP.Cloud
{
    /// <summary>
    /// Manages shared access storage operations, supporting multiple providers.
    /// </summary>
    public class SharedAccessStorageManager
    {
        private static SharedAccessStorageManager _instance;
        private ISharedAccessStorageProvider _currentProvider;
        private bool _initialized = false;

        public bool IsInitialized => _initialized && _currentProvider?.IsInitialized == true;
        public string CurrentProvider { get; private set; } = "None";

        public UnityEvent OnInitialized { get; } = new UnityEvent();
        public UnityEvent OnUploadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnUploadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnUploadFailed { get; } = new UnityEvent<Exception>();
        public UnityEvent OnDownloadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnDownloadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnDownloadFailed { get; } = new UnityEvent<Exception>();

        private SharedAccessStorageManager() { }

        public static SharedAccessStorageManager Instance => _instance ?? (_instance = new SharedAccessStorageManager());

        /// <summary>
        /// Initializes the shared access storage manager with the configured provider.
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                DebugConsole.Log("SharedAccessStorageManager already initialized.");
                return;
            }

            try
            {
                string provider = Configuration.GetCloudStorageProperty<string>("Provider");
                DebugConsole.Log($"SharedAccessStorageManager: Initializing with provider: {provider}");

                switch (provider?.ToLower())
                {
                    case "googledrive":
                        InitializeGoogleDrive();
                        break;
                    case "httpcloud":
                    case "http":
                    case "httpsharedstorage":
                        InitializeHttpSharedStorage();
                        break;
                    default:
                        DebugConsole.LogWarning($"SharedAccessStorageManager: Unknown provider '{provider}', falling back to HTTP Shared Storage");
                        InitializeHttpSharedStorage();
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"SharedAccessStorageManager: Initialization failed: {ex.Message}", false);
            }
        }

        private void InitializeGoogleDrive()
        {
            try
            {
                var googleDriveProvider = new GoogleDriveProvider();
                googleDriveProvider.Initialize();
                
                if (googleDriveProvider.IsInitialized)
                {
                    _currentProvider = (ISharedAccessStorageProvider)googleDriveProvider;
                    CurrentProvider = "GoogleDrive";
                    ConnectEvents();
                    _initialized = true;
                    DebugConsole.Log("SharedAccessStorageManager: Successfully initialized with Google Drive");
                    OnInitialized.Invoke();
                }
                else
                {
                    DebugConsole.LogError("SharedAccessStorageManager: Google Drive initialization failed", false);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"SharedAccessStorageManager: Google Drive initialization error: {ex.Message}", false);
            }
        }

        private void InitializeHttpSharedStorage()
        {
            try
            {
                var httpStorageProvider = new HttpSharedStorageProvider();
                httpStorageProvider.Initialize();
                
                if (httpStorageProvider.IsInitialized)
                {
                    _currentProvider = httpStorageProvider;
                    CurrentProvider = "HttpSharedStorage";
                    ConnectEvents();
                    _initialized = true;
                    DebugConsole.Log("SharedAccessStorageManager: Successfully initialized with HTTP Shared Storage");
                    OnInitialized.Invoke();
                }
                else
                {
                    DebugConsole.LogError("SharedAccessStorageManager: HTTP Shared Storage initialization failed", false);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"SharedAccessStorageManager: HTTP Shared Storage initialization error: {ex.Message}", false);
            }
        }

        private void ConnectEvents()
        {
            if (_currentProvider == null) return;

            _currentProvider.OnUploadStarted.AddListener(() => OnUploadStarted.Invoke());
            _currentProvider.OnUploadFinished.AddListener((result) => OnUploadFinished.Invoke(result));
            _currentProvider.OnUploadFailed.AddListener((ex) => OnUploadFailed.Invoke(ex));
            _currentProvider.OnDownloadStarted.AddListener(() => OnDownloadStarted.Invoke());
            _currentProvider.OnDownloadFinished.AddListener((result) => OnDownloadFinished.Invoke(result));
            _currentProvider.OnDownloadFailed.AddListener((ex) => OnDownloadFailed.Invoke(ex));
        }

        /// <summary>
        /// Uploads a file using the current provider.
        /// </summary>
        public void UploadFile(string localFilePath, string remoteFileName = null)
        {
            if (!IsInitialized)
            {
                DebugConsole.LogError("SharedAccessStorageManager: Not initialized!", false);
                OnUploadFailed?.Invoke(new InvalidOperationException("Shared access storage not initialized"));
                return;
            }

            _currentProvider.UploadFile(localFilePath, remoteFileName);
        }

        /// <summary>
        /// Downloads a file using the current provider.
        /// </summary>
        public void DownloadFile(string remoteFileName, string localFilePath)
        {
            if (!IsInitialized)
            {
                DebugConsole.LogError("SharedAccessStorageManager: Not initialized!", false);
                OnDownloadFailed?.Invoke(new InvalidOperationException("Shared access storage not initialized"));
                return;
            }

            _currentProvider.DownloadFile(remoteFileName, localFilePath);
        }

        /// <summary>
        /// Gets storage quota information if supported by the provider.
        /// </summary>
        public string GetQuotaInfo()
        {
            if (!IsInitialized)
                return "Shared access storage not initialized";

            return _currentProvider.GetQuotaInfo();
        }

        /// <summary>
        /// Lists available files if supported by the provider.
        /// </summary>
        public string[] ListFiles()
        {
            if (!IsInitialized)
                return new string[0];

            return _currentProvider.ListFiles();
        }

        /// <summary>
        /// Switches to a different shared access storage provider.
        /// </summary>
        public void SwitchProvider(string providerName)
        {
            DebugConsole.Log($"SharedAccessStorageManager: Switching provider to {providerName}");
            
            // Disconnect current provider events
            if (_currentProvider != null)
            {
                _currentProvider.OnUploadStarted.RemoveAllListeners();
                _currentProvider.OnUploadFinished.RemoveAllListeners();
                _currentProvider.OnUploadFailed.RemoveAllListeners();
                _currentProvider.OnDownloadStarted.RemoveAllListeners();
                _currentProvider.OnDownloadFinished.RemoveAllListeners();
                _currentProvider.OnDownloadFailed.RemoveAllListeners();
            }

            _initialized = false;
            _currentProvider = null;
            CurrentProvider = "None";

            // Update configuration
            try
            {
                Configuration.Instance.Host.CloudStorage.Provider = providerName;
                Configuration.Instance.Save();
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"SharedAccessStorageManager: Failed to update configuration: {ex.Message}", false);
            }

            // Initialize new provider
            Initialize();
        }
    }
}
