using ONI_MP.DebugTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ONI_MP.SharedStorage
{
    /// <summary>
    /// Dedicated client for communicating with StorageServer APIs.
    /// Provides direct HTTP client functionality for storage operations.
    /// </summary>
    public class StorageServerClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _authToken;
        private bool _disposed = false;

        public string BaseUrl => _baseUrl;
        public bool IsConnected { get; private set; }

        private const int TIMEOUT_SECONDS = 30;
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int RETRY_DELAY_MS = 1000;

        public StorageServerClient(string baseUrl, string authToken = null)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _authToken = authToken; // Authentication token for secure communication

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS);

            // Add authentication header if token is provided
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ONI-MP-Client/1.0");
            if (!string.IsNullOrEmpty(_authToken))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _authToken);
            }
        }

        /// <summary>
        /// Tests connection to the storage server
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/health");
                IsConnected = response.IsSuccessStatusCode;
                
                if (IsConnected)
                {
                    DebugConsole.Log($"StorageServerClient: Successfully connected to {_baseUrl}");
                }
                else
                {
                    DebugConsole.LogWarning($"StorageServerClient: Connection failed. Server responded with {response.StatusCode}");
                }

                return IsConnected;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                DebugConsole.LogError($"StorageServerClient: Connection test failed: {ex.Message}", false);
                return false;
            }
        }

        /// <summary>
        /// Creates a new session on the server
        /// </summary>
        public async Task<string> CreateSessionAsync(string sessionId = null)
        {
            try
            {
                var sessionData = new
                {
                    sessionId = sessionId ?? GenerateSessionId(),
                    clientVersion = "1.0",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                var json = JsonConvert.SerializeObject(sessionData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await RetryOperationAsync(async () => 
                    await _httpClient.PostAsync($"{_baseUrl}/session", content), 
                    "Create session");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<SessionResponse>(responseContent);
                    
                    string createdSessionId = result?.sessionId ?? sessionData.sessionId;
                    DebugConsole.Log($"StorageServerClient: Session created: {createdSessionId}");
                    return createdSessionId;
                }
                else
                {
                    DebugConsole.LogError($"StorageServerClient: Failed to create session: {response.ReasonPhrase}", false);
                    return null;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerClient: Session creation failed: {ex.Message}", false);
                return null;
            }
        }

        /// <summary>
        /// Uploads a file to the storage server
        /// </summary>
        public async Task<bool> UploadFileAsync(string localFilePath, string remoteFileName, string sessionId)
        {
            try
            {
                if (!File.Exists(localFilePath))
                {
                    DebugConsole.LogError($"StorageServerClient: Local file not found: {localFilePath}", false);
                    return false;
                }

                var fileBytes = File.ReadAllBytes(localFilePath);
                
                using (var form = new MultipartFormDataContent())
                {
                    form.Add(new StringContent(sessionId), "sessionId");
                    form.Add(new StringContent(remoteFileName), "fileName");
                    form.Add(new ByteArrayContent(fileBytes), "file", remoteFileName);

                    var response = await RetryOperationAsync(async () =>
                        await _httpClient.PostAsync($"{_baseUrl}/upload", form),
                        "Upload file");

                    if (response.IsSuccessStatusCode)
                    {
                        DebugConsole.Log($"StorageServerClient: Successfully uploaded {remoteFileName} ({fileBytes.Length} bytes)");
                        return true;
                    }
                    else
                    {
                        DebugConsole.LogError($"StorageServerClient: Upload failed: {response.ReasonPhrase}", false);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerClient: Upload exception: {ex.Message}", false);
                return false;
            }
        }

        /// <summary>
        /// Downloads a file from the storage server
        /// </summary>
        public async Task<bool> DownloadFileAsync(string remoteFileName, string localFilePath, string sessionId)
        {
            try
            {
                var url = $"{_baseUrl}/download?sessionId={Uri.EscapeDataString(sessionId)}&fileName={Uri.EscapeDataString(remoteFileName)}";
                
                var response = await RetryOperationAsync(async () =>
                    await _httpClient.GetAsync(url),
                    "Download file");

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    
                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(localFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    File.WriteAllBytes(localFilePath, fileBytes);
                    DebugConsole.Log($"StorageServerClient: Successfully downloaded {remoteFileName} to {localFilePath} ({fileBytes.Length} bytes)");
                    return true;
                }
                else
                {
                    DebugConsole.LogError($"StorageServerClient: Download failed: {response.ReasonPhrase}", false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerClient: Download exception: {ex.Message}", false);
                return false;
            }
        }

        /// <summary>
        /// Lists files available in a session
        /// </summary>
        public async Task<List<string>> ListFilesAsync(string sessionId)
        {
            try
            {
                var url = $"{_baseUrl}/files?sessionId={Uri.EscapeDataString(sessionId)}";
                
                var response = await RetryOperationAsync(async () =>
                    await _httpClient.GetAsync(url),
                    "List files");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<FilesResponse>(responseContent);
                    
                    var files = new List<string>();
                    if (result?.files != null)
                    {
                        files.AddRange(result.files);
                    }
                    
                    DebugConsole.Log($"StorageServerClient: Listed {files.Count} files for session {sessionId}");
                    return files;
                }
                else
                {
                    DebugConsole.LogError($"StorageServerClient: Failed to list files: {response.ReasonPhrase}", false);
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerClient: List files exception: {ex.Message}", false);
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets server status and quota information
        /// </summary>
        public async Task<ServerStatus> GetServerStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/status");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ServerStatus>(responseContent);
                    return result;
                }
                else
                {
                    DebugConsole.LogError($"StorageServerClient: Failed to get server status: {response.ReasonPhrase}", false);
                    return null;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerClient: Get server status exception: {ex.Message}", false);
                return null;
            }
        }

        private async Task<HttpResponseMessage> RetryOperationAsync(Func<Task<HttpResponseMessage>> operation, string operationName)
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
                    
                    if (attempt < MAX_RETRY_ATTEMPTS)
                    {
                        DebugConsole.LogWarning($"StorageServerClient: {operationName} attempt {attempt} failed: {ex.Message}. Retrying...");
                        await Task.Delay(RETRY_DELAY_MS * attempt); // Exponential backoff
                    }
                }
            }

            throw lastException ?? new Exception($"Operation {operationName} failed after {MAX_RETRY_ATTEMPTS} attempts");
        }

        private string GenerateSessionId()
        {
            try
            {
                // Try to use Steam user ID
                if (SteamManager.Initialized)
                {
                    var steamId = Steamworks.SteamUser.GetSteamID().m_SteamID;
                    return $"user_{steamId}_{DateTimeOffset.UtcNow.Ticks}";
                }
            }
            catch
            {
                // Fall back to random ID if Steam is not available
            }

            return $"client_{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.Ticks}";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Server status information
    /// </summary>
    public class ServerStatus
    {
        public string Version { get; set; }
        public bool IsOnline { get; set; }
        public long TotalSpaceBytes { get; set; }
        public long UsedSpaceBytes { get; set; }
        public long AvailableSpaceBytes { get; set; }
        public int ActiveSessions { get; set; }
        public DateTime ServerTime { get; set; }

        public string GetQuotaInfo()
        {
            if (TotalSpaceBytes > 0)
            {
                var usedMB = UsedSpaceBytes / (1024 * 1024);
                var totalMB = TotalSpaceBytes / (1024 * 1024);
                var percentUsed = (double)UsedSpaceBytes / TotalSpaceBytes * 100;
                return $"Storage: {usedMB:N0} MB / {totalMB:N0} MB ({percentUsed:F1}%)";
            }
            return "Storage: Information not available";
        }
    }

    /// <summary>
    /// Response from session creation
    /// </summary>
    public class SessionResponse
    {
        public string sessionId { get; set; }
        public string status { get; set; }
    }

    /// <summary>
    /// Response from file listing
    /// </summary>
    public class FilesResponse
    {
        public string[] files { get; set; }
        public int count { get; set; }
    }
}
