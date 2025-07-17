using ONI_MP.DebugTools;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ONI_MP.SharedStorage
{
    /// <summary>
    /// Manages the embedded Storage Server lifecycle.
    /// Handles starting, stopping, and health checking of the embedded HTTP server.
    /// </summary>
    public static class StorageServerManager
    {
        private static EmbeddedStorageServer _embeddedServer;
        private static bool _isRunning = false;
        private static string _currentServerUrl = "http://localhost:29600";
        
        public static bool IsRunning => _isRunning && _embeddedServer != null && _embeddedServer.IsRunning;
        public static string ServerUrl => _currentServerUrl;
        public static string ExternalUrl => _embeddedServer?.ExternalUrl;
        public static string ExternalIP => _embeddedServer?.ExternalIP;
        public static bool UPnPEnabled => _embeddedServer?.UPnPEnabled ?? false;
        public static string AuthToken => _embeddedServer?.AuthToken;

        /// <summary>
        /// Starts the embedded Storage Server
        /// </summary>
        public static async Task<bool> StartServerAsync(int port = 29600)
        {
            if (IsRunning)
            {
                DebugConsole.Log($"StorageServerManager: Server already running on {_currentServerUrl}");
                return true;
            }

            try
            {
                DebugConsole.Log($"StorageServerManager: Starting embedded server on port {port}");

                // Update the server URL with the specified port
                _currentServerUrl = $"http://localhost:{port}";

                // Create and start the embedded server
                _embeddedServer = new EmbeddedStorageServer(port);
                
                bool success = await _embeddedServer.StartAsync();
                if (success)
                {
                    _isRunning = true;
                    DebugConsole.Log($"StorageServerManager: Embedded server started successfully on {_currentServerUrl}");
                    
                    // Update configuration to use this server
                    Configuration.Instance.Host.CloudStorage.StorageServer.HttpServerUrl = _currentServerUrl;
                    Configuration.Instance.Host.CloudStorage.StorageServer.AuthToken = _embeddedServer.AuthToken;
                    Configuration.Instance.Save();
                    
                    DebugConsole.Log($"StorageServerManager: Auth token: {_embeddedServer.AuthToken}");
                    
                    return true;
                }
                else
                {
                    DebugConsole.LogError("StorageServerManager: Failed to start embedded server", false);
                    StopServer();
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerManager: Failed to start server: {ex.Message}", false);
                StopServer();
                return false;
            }
        }

        /// <summary>
        /// Stops the embedded Storage Server
        /// </summary>
        public static void StopServer()
        {
            try
            {
                if (_embeddedServer != null && _isRunning)
                {
                    DebugConsole.Log("StorageServerManager: Stopping embedded server...");
                    _embeddedServer.StopAsync().Wait(5000); // Wait up to 5 seconds
                    _embeddedServer.Dispose();
                    _embeddedServer = null;
                    _isRunning = false;
                    DebugConsole.Log("StorageServerManager: Embedded server stopped");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"StorageServerManager: Error stopping server: {ex.Message}", false);
            }
            finally
            {
                _embeddedServer?.Dispose();
                _embeddedServer = null;
                _isRunning = false;
            }
        }

        /// <summary>
        /// Sets a custom server URL for connecting to external servers
        /// </summary>
        public static void SetServerUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                DebugConsole.LogError("StorageServerManager: Invalid server URL", false);
                return;
            }

            _currentServerUrl = url.TrimEnd('/');
            if (!_currentServerUrl.StartsWith("http://") && !_currentServerUrl.StartsWith("https://"))
            {
                _currentServerUrl = "http://" + _currentServerUrl;
            }

            // Update configuration
            Configuration.Instance.Host.CloudStorage.StorageServer.HttpServerUrl = _currentServerUrl;
            Configuration.Instance.Save();

            DebugConsole.Log($"StorageServerManager: Server URL set to {_currentServerUrl}");
        }

        /// <summary>
        /// Performs a health check on the server
        /// </summary>
        public static async Task<bool> PerformHealthCheck()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync($"{_currentServerUrl}/health");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cleanup when the application is shutting down
        /// </summary>
        public static void Cleanup()
        {
            StopServer();
        }
    }
}
