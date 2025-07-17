using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Components;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace ONI_MP.SharedStorage
{
    /// <summary>
    /// HTTP-based shared storage provider for dedicated file servers.
    /// Allows multiplayer save file sharing through a self-hosted server.
    /// </summary>
    public class StorageServerProvider : ISharedStorageProvider
    {
        public bool IsInitialized { get; private set; }
        public string ProviderName => "StorageServer";

        public UnityEvent OnUploadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnUploadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnUploadFailed { get; } = new UnityEvent<Exception>();
        public UnityEvent OnDownloadStarted { get; } = new UnityEvent();
        public UnityEvent<string> OnDownloadFinished { get; } = new UnityEvent<string>();
        public UnityEvent<Exception> OnDownloadFailed { get; } = new UnityEvent<Exception>();
        public UnityEvent OnInitializationComplete { get; } = new UnityEvent();

        private string _serverUrl;
        private string _sessionId;
        private string _authToken;
        private bool _isUploading = false;
        private bool _isDownloading = false;
        private HttpClient _httpClient;

        private const int TIMEOUT_SECONDS = 30;
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int RETRY_DELAY_MS = 1000;

        public void Initialize()
        {
            try
            {
                // Use the server URL from StorageServerManager (which handles configuration)
                _serverUrl = StorageServerManager.ServerUrl;
                _sessionId = Configuration.GetStorageServerProperty<string>("SessionId");
                
                // Get auth token from StorageServerManager if available, otherwise from configuration
                _authToken = StorageServerManager.AuthToken ?? Configuration.GetStorageServerProperty<string>("AuthToken");

                if (string.IsNullOrEmpty(_serverUrl))
                {
                    // Fallback to configuration if manager doesn't have a URL
                    _serverUrl = Configuration.GetStorageServerProperty<string>("HttpServerUrl");
                    if (string.IsNullOrEmpty(_serverUrl))
                    {
                        _serverUrl = "http://localhost:29600"; // Default fallback to match embedded server
                    }
                }

                if (string.IsNullOrEmpty(_sessionId))
                {
                    // Generate a session ID based on Steam user or lobby
                    _sessionId = GenerateSessionId();
                    DebugConsole.Log($"StorageServerProvider: Generated session ID: {_sessionId}");
                }

                // Normalize server URL
                if (!_serverUrl.StartsWith("http://") && !_serverUrl.StartsWith("https://"))
                {
                    _serverUrl = "http://" + _serverUrl;
                }
                
                if (_serverUrl.EndsWith("/"))
                {
                    _serverUrl = _serverUrl.TrimEnd('/');
                }

                DebugConsole.Log($"StorageServerProvider: Initializing with server URL: {_serverUrl}");

                // Initialize HTTP client
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS);
                
                // Add required headers including authentication
                _httpClient.DefaultRequestHeaders.Add("X-Session-ID", _sessionId);
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "ONI-MP-Client/1.0");
                
                // Add auth token if available
                if (!string.IsNullOrEmpty(_authToken))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _authToken);
                    DebugConsole.Log($"StorageServerProvider: Auth token added to headers");
                }
                else
                {
                    DebugConsole.LogWarning("StorageServerProvider: No auth token available for authentication");
                }

                // Test connection and create/join session
                Task.Run(async () => await InitializeSessionAsync());
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerProvider: Initialization failed: {ex.Message}", false);
                throw;
            }
        }

        private string GenerateSessionId()
        {
            // Try to use Steam user ID
            try
            {
                var steamId = Steamworks.SteamUser.GetSteamID().m_SteamID;
                return $"user_{steamId}_{System.DateTime.UtcNow.Ticks}";
            }
            catch
            {
                return $"session_{Guid.NewGuid():N}";
            }
        }

        private async Task InitializeSessionAsync()
        {
            try
            {
                // First validate server connectivity
                if (!await ValidateServerConnectivity())
                {
                    DebugConsole.LogError("StorageServerProvider: Server connectivity validation failed", false);
                    IsInitialized = false;
                    return;
                }

                var sessionData = new
                {
                    sessionId = _sessionId,
                    host = Environment.MachineName,
                    timestamp = System.DateTime.UtcNow.ToString("O"),
                    clientVersion = "1.0.0"
                };

                var json = JsonConvert.SerializeObject(sessionData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await RetryOperation(async () => 
                    await _httpClient.PostAsync($"{_serverUrl}/session", content), 
                    "Session initialization");

                if (response.IsSuccessStatusCode)
                {
                    IsInitialized = true;
                    DebugConsole.Log($"StorageServerProvider: Successfully initialized with session {_sessionId}");
                    OnInitializationComplete?.Invoke();
                }
                else
                {
                    DebugConsole.LogError($"StorageServerProvider: Failed to initialize session: {response.ReasonPhrase}", false);
                    IsInitialized = false;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerProvider: Session initialization failed: {ex.Message}", false);
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

            if (_isUploading)
            {
                DebugConsole.LogWarning("StorageServerProvider: Upload already in progress");
                return;
            }

            if (!File.Exists(localFilePath))
            {
                OnUploadFailed?.Invoke(new FileNotFoundException($"File not found: {localFilePath}"));
                return;
            }

            if (string.IsNullOrEmpty(remoteFileName))
            {
                remoteFileName = Path.GetFileName(localFilePath);
            }

            Task.Run(async () => await UploadFileAsync(localFilePath, remoteFileName));
        }

        private async Task UploadFileAsync(string localFilePath, string remoteFileName)
        {
            _isUploading = true;
            OnUploadStarted?.Invoke();

            try
            {
                var fileBytes = File.ReadAllBytes(localFilePath);
                DebugConsole.Log($"StorageServerProvider: Uploading {remoteFileName} ({fileBytes.Length} bytes)");

                using (var form = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    form.Add(fileContent, "file", remoteFileName);

                    var response = await _httpClient.PostAsync($"{_serverUrl}/upload", form);

                    if (response.IsSuccessStatusCode)
                    {
                        DebugConsole.Log($"StorageServerProvider: Successfully uploaded {remoteFileName}");
                        
                        // Notify on main thread
                        MainThreadExecutor.dispatcher.QueueEvent(() =>
                        {
                            OnUploadFinished?.Invoke(remoteFileName);
                            MultiplayerOverlay.Show($"Upload complete: {remoteFileName}");
                        });
                    }
                    else
                    {
                        var error = new Exception($"Upload failed: {response.ReasonPhrase}");
                        DebugConsole.LogError($"StorageServerProvider: {error.Message}", false);
                        
                        MainThreadExecutor.dispatcher.QueueEvent(() =>
                        {
                            OnUploadFailed?.Invoke(error);
                            MultiplayerOverlay.Show($"Upload failed: {response.ReasonPhrase}");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerProvider: Upload exception: {ex.Message}", false);
                MainThreadExecutor.dispatcher.QueueEvent(() =>
                {
                    OnUploadFailed?.Invoke(ex);
                });
            }
            finally
            {
                _isUploading = false;
            }
        }

        public void DownloadFile(string remoteFileName, string localFilePath)
        {
            if (!IsInitialized)
            {
                OnDownloadFailed?.Invoke(new InvalidOperationException("Provider not initialized"));
                return;
            }

            if (_isDownloading)
            {
                DebugConsole.LogWarning("StorageServerProvider: Download already in progress");
                return;
            }

            Task.Run(async () => await DownloadFileAsync(remoteFileName, localFilePath));
        }

        private async Task DownloadFileAsync(string remoteFileName, string localFilePath)
        {
            _isDownloading = true;
            OnDownloadStarted?.Invoke();

            try
            {
                DebugConsole.Log($"StorageServerProvider: Downloading {remoteFileName}");

                var encodedFileName = Uri.EscapeDataString(remoteFileName);
                var response = await _httpClient.GetAsync($"{_serverUrl}/download/{encodedFileName}");

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();

                    // Ensure destination directory exists
                    var directory = Path.GetDirectoryName(localFilePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Write file
                    File.WriteAllBytes(localFilePath, fileBytes);

                    DebugConsole.Log($"StorageServerProvider: Successfully downloaded {remoteFileName} to {localFilePath}");
                    
                    MainThreadExecutor.dispatcher.QueueEvent(() =>
                    {
                        OnDownloadFinished?.Invoke(localFilePath);
                        MultiplayerOverlay.Show($"Download complete: {remoteFileName}");
                    });
                }
                else
                {
                    var error = new Exception($"Download failed: {response.ReasonPhrase}");
                    DebugConsole.LogError($"StorageServerProvider: {error.Message}", false);
                    
                    MainThreadExecutor.dispatcher.QueueEvent(() =>
                    {
                        OnDownloadFailed?.Invoke(error);
                        MultiplayerOverlay.Show($"Download failed: {response.ReasonPhrase}");
                    });
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerProvider: Download exception: {ex.Message}", false);
                MainThreadExecutor.dispatcher.QueueEvent(() =>
                {
                    OnDownloadFailed?.Invoke(ex);
                });
            }
            finally
            {
                _isDownloading = false;
            }
        }

        public string GetQuotaInfo()
        {
            if (!IsInitialized)
                return "Provider not initialized";

            // HTTP provider doesn't have a quota system like traditional cloud storage
            return "HTTP Shared Storage - No quota limits (limited by server disk space)";
        }

        public string[] ListFiles()
        {
            if (!IsInitialized)
                return new string[0];

            // This is a synchronous method but we need async for HTTP
            // Return empty array and use async method instead
            DebugConsole.LogWarning("StorageServerProvider: ListFiles() called - consider using ListFilesAsync()");
            return new string[0];
        }

        /// <summary>
        /// Retry policy for failed HTTP operations
        /// </summary>
        private async Task<T> RetryOperation<T>(Func<Task<T>> operation, string operationName)
        {
            Exception lastException = null;
            
            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    DebugConsole.LogWarning($"StorageServerProvider: {operationName} attempt {attempt} failed: {ex.Message}");
                    
                    if (attempt < MAX_RETRY_ATTEMPTS)
                    {
                        await Task.Delay(RETRY_DELAY_MS * attempt); // Exponential backoff
                    }
                }
            }
            
            throw lastException ?? new Exception($"{operationName} failed after {MAX_RETRY_ATTEMPTS} attempts");
        }

        /// <summary>
        /// Validates server connectivity
        /// </summary>
        private async Task<bool> ValidateServerConnectivity()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public void ListFilesAsync(System.Action<string[]> callback)
        {
            if (!IsInitialized)
            {
                callback?.Invoke(new string[0]);
                return;
            }

            Task.Run(async () => await ListFilesAsyncInternal(callback));
        }

        private async Task ListFilesAsyncInternal(System.Action<string[]> callback)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/files/{_sessionId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var fileListResponse = JsonConvert.DeserializeObject<FileListResponse>(json);
                    
                    var fileNames = new string[fileListResponse.files.Length];
                    for (int i = 0; i < fileListResponse.files.Length; i++)
                    {
                        fileNames[i] = fileListResponse.files[i].originalName;
                    }

                    callback?.Invoke(fileNames);
                }
                else
                {
                    DebugConsole.LogError($"StorageServerProvider: Failed to list files: {response.ReasonPhrase}", false);
                    callback?.Invoke(new string[0]);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerProvider: Failed to list files: {ex.Message}", false);
                callback?.Invoke(new string[0]);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        [System.Serializable]
        private class FileListResponse
        {
            public string sessionId { get; set; }
            public FileInfo[] files { get; set; }
        }

        [System.Serializable]
        private class FileInfo
        {
            public string filename { get; set; }
            public string originalName { get; set; }
            public long size { get; set; }
            public string uploadedAt { get; set; }
        }
    }
}
