using ONI_MP.DebugTools;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace ONI_MP.SharedStorage
{
    /// <summary>
    /// Manages shared access storage operations, supporting multiple providers.
    /// </summary>
    public class SharedStorageManager
    {
        private static SharedStorageManager _instance;
        private ISharedStorageProvider _currentProvider;
        private bool _initialized = false;

        public bool IsInitialized => _initialized && _currentProvider?.IsInitialized == true;
        public string CurrentProvider { get; private set; } = "SteamP2P";

        public UnityEvent OnInitialized { get; } = new UnityEvent();
        public UnityEvent OnUploadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnUploadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnUploadFailed { get; } = new UnityEvent<Exception>();
        public UnityEvent OnDownloadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnDownloadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnDownloadFailed { get; } = new UnityEvent<Exception>();

        private SharedStorageManager() { }

        public static SharedStorageManager Instance => _instance ?? (_instance = new SharedStorageManager());

        /// <summary>
        /// Initializes the shared access storage manager with the configured provider.
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                DebugConsole.Log("SharedStorageManager already initialized.");
                return;
            }

            try
            {
                string provider = Configuration.GetCloudStorageProperty<string>("Provider");
                DebugConsole.Log($"SharedStorageManager: Initializing with provider: {provider}");

                switch (provider?.ToLower())
                {
                    case "googledrive":
                        InitializeGoogleDrive();
                        break;
                    case "steamp2p":
                        InitializeSteamP2P();
                        break;
                    default:
                        DebugConsole.LogWarning($"SharedStorageManager: Unknown provider '{provider}', falling back to SteamP2P");
                        InitializeSteamP2P();
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"SharedStorageManager: Initialization failed: {ex.Message}", false);
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
                    _currentProvider = (ISharedStorageProvider)googleDriveProvider;
                    CurrentProvider = "GoogleDrive";
                    ConnectEvents();
                    _initialized = true;
                    DebugConsole.Log("SharedStorageManager: Successfully initialized with Google Drive");
                    OnInitialized.Invoke();
                }
                else
                {
                    DebugConsole.LogError("SharedStorageManager: Google Drive initialization failed", false);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"SharedStorageManager: Google Drive initialization error: {ex.Message}", false);
            }
        }

        private void InitializeSteamP2P()
        {
            try
            {
                DebugConsole.Log("SharedStorageManager: Initializing Steam P2P Storage Provider");
                DebugConsole.Log($"SharedStorageManager: Steam initialized: {SteamManager.Initialized}");
                
                var steamP2PProvider = new SteamP2PStorageProvider();
                
                // Set up callback for when async initialization completes
                steamP2PProvider.OnInitializationComplete.AddListener(() => {
                    DebugConsole.Log($"SharedStorageManager: Steam P2P provider initialization complete. IsInitialized: {steamP2PProvider.IsInitialized}");
                    
                    if (steamP2PProvider.IsInitialized)
                    {
                        DebugConsole.Log("SharedStorageManager: Steam P2P Storage initialization completed successfully");
                        _initialized = true;
                        OnInitialized.Invoke();
                    }
                    else
                    {
                        DebugConsole.LogError("SharedStorageManager: Steam P2P Storage initialization failed", false);
                        // Still need to fire OnInitialized so SaveLoaderPatch doesn't hang waiting
                        DebugConsole.Log("SharedStorageManager: Marking as initialized despite provider failure to allow lobby creation");
                        _initialized = true;
                        OnInitialized.Invoke();
                    }
                });
                
                steamP2PProvider.Initialize();
                
                // Set up the provider but don't mark as initialized until async completes
                _currentProvider = steamP2PProvider;
                CurrentProvider = "SteamP2P";
                ConnectEvents();
                DebugConsole.Log("SharedStorageManager: Steam P2P Storage provider set up, waiting for initialization to complete");
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"SharedStorageManager: Steam P2P Storage initialization error: {ex.Message}", false);
                DebugConsole.LogError($"SharedStorageManager: Steam P2P Stack trace: {ex.StackTrace}", false);
                // Still fire OnInitialized to prevent hanging
                _initialized = true;
                OnInitialized.Invoke();
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
                DebugConsole.LogError("SharedStorageManager: Not initialized!", false);
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
                DebugConsole.LogError("SharedStorageManager: Not initialized!", false);
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
            DebugConsole.Log($"SharedStorageManager: Switching provider to {providerName}");
            
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
                DebugConsole.LogError($"SharedStorageManager: Failed to update configuration: {ex.Message}", false);
            }

            // Initialize new provider
            Initialize();
        }
    }
}
