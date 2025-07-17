using ONI_MP.DebugTools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ONI_MP.SharedStorage
{
    /// <summary>
    /// High-level utility for working with StorageServer
    /// Provides simplified methods for common operations
    /// </summary>
    public static class StorageServerUtils
    {
        private static StorageServerClient _client;
        private static string _currentSessionId;

        /// <summary>
        /// Initialize connection to the storage server using configuration
        /// </summary>
        public static async Task<bool> InitializeAsync()
        {
            try
            {
                var serverUrl = Configuration.GetStorageServerProperty<string>("HttpServerUrl");
                var authToken = Configuration.GetStorageServerProperty<string>("AuthToken");
                var sessionId = Configuration.GetStorageServerProperty<string>("SessionId");

                if (string.IsNullOrEmpty(serverUrl))
                {
                    DebugConsole.LogError("StorageServerUtils: HttpServerUrl not configured", false);
                    return false;
                }

                // Dispose existing client if any
                _client?.Dispose();

                _client = new StorageServerClient(serverUrl, authToken);

                // Test connection
                if (!await _client.TestConnectionAsync())
                {
                    DebugConsole.LogError("StorageServerUtils: Failed to connect to storage server", false);
                    return false;
                }

                // Create or reuse session
                if (string.IsNullOrEmpty(sessionId))
                {
                    _currentSessionId = await _client.CreateSessionAsync();
                    if (string.IsNullOrEmpty(_currentSessionId))
                    {
                        DebugConsole.LogError("StorageServerUtils: Failed to create session", false);
                        return false;
                    }

                    // Save session ID to configuration
                    Configuration.Instance.Host.CloudStorage.StorageServer.SessionId = _currentSessionId;
                    Configuration.Instance.Save();
                }
                else
                {
                    _currentSessionId = sessionId;
                }

                DebugConsole.Log($"StorageServerUtils: Successfully initialized with session {_currentSessionId}");
                return true;
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerUtils: Initialization failed: {ex.Message}", false);
                return false;
            }
        }

        /// <summary>
        /// Upload a file to the storage server
        /// </summary>
        public static async Task<bool> UploadFileAsync(string localFilePath, string remoteFileName = null)
        {
            if (_client == null || string.IsNullOrEmpty(_currentSessionId))
            {
                DebugConsole.LogError("StorageServerUtils: Not initialized. Call InitializeAsync() first.", false);
                return false;
            }

            if (string.IsNullOrEmpty(remoteFileName))
            {
                remoteFileName = System.IO.Path.GetFileName(localFilePath);
            }

            return await _client.UploadFileAsync(localFilePath, remoteFileName, _currentSessionId);
        }

        /// <summary>
        /// Download a file from the storage server
        /// </summary>
        public static async Task<bool> DownloadFileAsync(string remoteFileName, string localFilePath)
        {
            if (_client == null || string.IsNullOrEmpty(_currentSessionId))
            {
                DebugConsole.LogError("StorageServerUtils: Not initialized. Call InitializeAsync() first.", false);
                return false;
            }

            return await _client.DownloadFileAsync(remoteFileName, localFilePath, _currentSessionId);
        }

        /// <summary>
        /// List all files in the current session
        /// </summary>
        public static async Task<List<string>> ListFilesAsync()
        {
            if (_client == null || string.IsNullOrEmpty(_currentSessionId))
            {
                DebugConsole.LogError("StorageServerUtils: Not initialized. Call InitializeAsync() first.", false);
                return new List<string>();
            }

            return await _client.ListFilesAsync(_currentSessionId);
        }

        /// <summary>
        /// Get server status and quota information
        /// </summary>
        public static async Task<ServerStatus> GetServerStatusAsync()
        {
            if (_client == null)
            {
                DebugConsole.LogError("StorageServerUtils: Not initialized. Call InitializeAsync() first.", false);
                return null;
            }

            return await _client.GetServerStatusAsync();
        }

        /// <summary>
        /// Get current session ID
        /// </summary>
        public static string GetCurrentSessionId()
        {
            return _currentSessionId;
        }

        /// <summary>
        /// Check if the utils are initialized
        /// </summary>
        public static bool IsInitialized => _client != null && !string.IsNullOrEmpty(_currentSessionId);

        /// <summary>
        /// Clean up resources
        /// </summary>
        public static void Cleanup()
        {
            _client?.Dispose();
            _client = null;
            _currentSessionId = null;
        }
    }
}
