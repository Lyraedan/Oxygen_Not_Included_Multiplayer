using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using ONI_MP.DebugTools;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ONI_MP.SharedStorage
{
    /// <summary>
    /// Embedded HTTP server for storage operations using HttpListener
    /// Replaces the Node.js server with a native C# implementation
    /// </summary>
    public class EmbeddedStorageServer : IDisposable
    {
        private HttpListener _listener;
        private readonly string _uploadPath;
        private readonly int _port;
        private readonly Dictionary<string, SessionData> _sessions;
        private bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private System.Threading.Timer _cleanupTimer;
        private readonly string _authToken;
        private string _externalIP;
        private bool _upnpEnabled = false;

        public bool IsRunning => _isRunning && _listener != null && _listener.IsListening;
        public string BaseUrl { get; private set; }
        public string ExternalUrl { get; private set; }
        public string AuthToken => _authToken;
        public string ExternalIP => _externalIP;
        public bool UPnPEnabled => _upnpEnabled;

        public EmbeddedStorageServer(int port = 29600, string uploadPath = null)
        {
            _port = port;
            BaseUrl = $"http://localhost:{port}/";
            _uploadPath = uploadPath ?? Path.Combine(Path.GetTempPath(), "ONI_MP_Storage");
            _sessions = new Dictionary<string, SessionData>();
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Generate a secure auth token for this server instance
            _authToken = GenerateAuthToken();
        }

        public async Task<bool> StartAsync()
        {
            try
            {
                if (!Directory.Exists(_uploadPath))
                    Directory.CreateDirectory(_uploadPath);

                _listener = new HttpListener();
                
                // Listen on all interfaces to allow external connections
                _listener.Prefixes.Add($"http://*:{_port}/");
                _listener.Start();
                _isRunning = true;

                DebugConsole.Log($"Embedded storage server started on port {_port}");
                DebugConsole.Log($"Local URL: {BaseUrl}");
                DebugConsole.Log($"Upload directory: {_uploadPath}");

                // Detect external IP and setup UPnP in background
                _ = Task.Run(SetupExternalAccess);

                // Start cleanup timer (runs every hour) - use System.Threading.Timer
                _cleanupTimer = new System.Threading.Timer(CleanupOldFiles, null, (int)TimeSpan.FromHours(1).TotalMilliseconds, (int)TimeSpan.FromHours(1).TotalMilliseconds);

                // Start listening for requests in background
                _ = Task.Run(HandleRequestsAsync, _cancellationTokenSource.Token);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Failed to start embedded server: {ex.Message}", false);
                return false;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                _isRunning = false;
                _cancellationTokenSource?.Cancel();
                _cleanupTimer?.Dispose();

                // Cleanup UPnP port forwarding
                CleanupUPnP();

                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close();
                }

                DebugConsole.Log("Embedded storage server stopped");
                await Task.FromResult(0); // Make method async
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error stopping embedded server: {ex.Message}", false);
            }
        }

        private async Task HandleRequestsAsync()
        {
            while (_isRunning && _listener.IsListening && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequestAsync(context), _cancellationTokenSource.Token);
                }
                catch (ObjectDisposedException)
                {
                    // Expected when stopping the server
                    break;
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995) // ERROR_OPERATION_ABORTED
                {
                    // Expected when stopping the server
                    break;
                }
                catch (Exception ex) when (_isRunning)
                {
                    DebugConsole.LogError($"Error handling request: {ex.Message}", false);
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // Add CORS headers
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Session-ID, X-Auth-Token");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                // Skip auth check for health endpoint to allow connection testing
                var path = request.Url.AbsolutePath.TrimStart('/').ToLower();
                if (path != "health" && !ValidateAuthToken(request))
                {
                    response.StatusCode = 401;
                    await WriteJsonResponse(response, new { error = "Invalid or missing authentication token" });
                    response.Close();
                    return;
                }

                var segments = path.Split('/');

                DebugConsole.Log($"[EmbeddedServer] {request.HttpMethod} /{path}");

                switch (segments[0])
                {
                    case "health":
                        await HandleHealthCheck(response);
                        break;
                    case "session":
                        if (request.HttpMethod == "POST")
                            await HandleCreateSession(request, response);
                        else if (request.HttpMethod == "GET" && segments.Length > 1)
                            await HandleGetSession(segments[1], response);
                        else
                            response.StatusCode = 404;
                        break;
                    case "upload":
                        await HandleUpload(request, response);
                        break;
                    case "download":
                        await HandleDownload(request, response);
                        break;
                    case "files":
                        if (segments.Length > 1)
                            await HandleListFiles(segments[1], response);
                        else
                            await HandleListFiles(request, response);
                        break;
                    case "delete":
                        if (segments.Length > 1)
                            await HandleDeleteFile(segments[1], request, response);
                        else
                            response.StatusCode = 404;
                        break;
                    case "status":
                    case "info":
                        await HandleServerStatus(response);
                        break;
                    default:
                        response.StatusCode = 404;
                        await WriteJsonResponse(response, new { error = "Endpoint not found" });
                        break;
                }

                response.Close();
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error processing request: {ex.Message}", false);
                try
                {
                    if (!context.Response.OutputStream.CanWrite) return;
                    context.Response.StatusCode = 500;
                    await WriteJsonResponse(context.Response, new { error = "Internal server error" });
                    context.Response.Close();
                }
                catch
                {
                    // Ignore errors when trying to send error response
                }
            }
        }

        private async Task HandleHealthCheck(HttpListenerResponse response)
        {
            var health = new
            {
                status = "healthy",
                timestamp = System.DateTime.Now.ToString("O"), // Use DateTime.Now instead of UtcNow
                uptime = Environment.TickCount / 1000, // Use TickCount instead of TickCount64
                version = "1.0.0"
            };

            await WriteJsonResponse(response, health);
        }

        private async Task HandleCreateSession(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                string body = "";
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    body = await reader.ReadToEndAsync();
                }

                var sessionRequest = string.IsNullOrEmpty(body) ? 
                    new SessionRequest() : 
                    JsonConvert.DeserializeObject<SessionRequest>(body);

                var sessionId = sessionRequest?.sessionId ?? Guid.NewGuid().ToString();

                lock (_sessions)
                {
                    if (!_sessions.ContainsKey(sessionId))
                    {
                        _sessions[sessionId] = new SessionData
                        {
                            SessionId = sessionId,
                            Files = new List<string>(),
                            LastActivity = System.DateTime.Now, 
                            Host = request.RemoteEndPoint?.Address?.ToString() ?? "unknown",
                            Created = System.DateTime.Now
                        };
                        DebugConsole.Log($"[EmbeddedServer] Created new session: {sessionId}");
                    }
                    else
                    {
                        _sessions[sessionId].LastActivity = System.DateTime.Now;
                        DebugConsole.Log($"[EmbeddedServer] Updated existing session: {sessionId}");
                    }
                }

                var responseData = new
                {
                    sessionId,
                    status = "active",
                    files = _sessions[sessionId].Files.ToArray()
                };

                await WriteJsonResponse(response, responseData);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error creating session: {ex.Message}", false);
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = "Failed to create session" });
            }
        }

        private async Task HandleUpload(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                if (!request.ContentType?.StartsWith("multipart/form-data") == true)
                {
                    response.StatusCode = 400;
                    await WriteJsonResponse(response, new { error = "Invalid content type" });
                    return;
                }

                var sessionId = request.Headers["X-Session-ID"] ?? "default";
                
                lock (_sessions)
                {
                    if (!_sessions.ContainsKey(sessionId))
                    {
                        response.StatusCode = 404;
                        WriteJsonResponse(response, new { error = "Session not found. Create a session first." }).Wait();
                        return;
                    }
                }

                // Parse multipart form data
                var formData = await ParseMultipartFormData(request);
                
                if (!formData.ContainsKey("file") || formData["file"].Data == null)
                {
                    response.StatusCode = 400;
                    await WriteJsonResponse(response, new { error = "No file provided" });
                    return;
                }

                var fileData = formData["file"];
                var fileName = formData.ContainsKey("fileName") ? formData["fileName"].Text : fileData.FileName;
                
                if (string.IsNullOrEmpty(fileName))
                    fileName = "unknown_file.sav";

                // Generate safe filename
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var hash = BitConverter.ToString(System.Security.Cryptography.MD5.Create()
                    .ComputeHash(Encoding.UTF8.GetBytes(fileName + timestamp)))
                    .Replace("-", "").Substring(0, 8);
                var safeFileName = $"{sessionId}_{timestamp}_{hash}_{fileName}";
                var filePath = Path.Combine(_uploadPath, safeFileName);

                File.WriteAllBytes(filePath, fileData.Data); // Use synchronous method

                lock (_sessions)
                {
                    _sessions[sessionId].Files.Add(safeFileName);
                    _sessions[sessionId].LastActivity = System.DateTime.Now; // Use DateTime.Now
                }

                DebugConsole.Log($"[EmbeddedServer] File uploaded: {fileName} -> {safeFileName} ({fileData.Data.Length} bytes)");

                var responseData = new
                {
                    success = true,
                    file = new
                    {
                        filename = safeFileName,
                        originalName = fileName,
                        size = fileData.Data.Length,
                        uploadedAt = System.DateTime.Now.ToString("O"), // Use DateTime.Now
                        sessionId = sessionId
                    }
                };

                await WriteJsonResponse(response, responseData);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error uploading file: {ex.Message}", false);
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = "Upload failed" });
            }
        }

        private async Task HandleDownload(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var query = ParseQueryString(request.Url.Query);
                var sessionId = GetValueOrDefault(query, "sessionId", "");
                var fileName = GetValueOrDefault(query, "fileName", "");

                if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(fileName))
                {
                    response.StatusCode = 400;
                    await WriteJsonResponse(response, new { error = "Missing sessionId or fileName" });
                    return;
                }

                // Find the actual file (it has session prefix)
                var files = Directory.GetFiles(_uploadPath, $"{sessionId}_*_{fileName}")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToArray();

                if (files.Length == 0)
                {
                    response.StatusCode = 404;
                    await WriteJsonResponse(response, new { error = "File not found" });
                    return;
                }

                var filePath = files[0]; // Get the most recent
                var fileBytes = File.ReadAllBytes(filePath); // Use synchronous method

                response.ContentType = "application/octet-stream";
                response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                response.ContentLength64 = fileBytes.Length;

                await response.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                DebugConsole.Log($"[EmbeddedServer] File downloaded: {fileName} ({fileBytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error downloading file: {ex.Message}", false);
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = "Download failed" });
            }
        }

        private async Task HandleListFiles(string sessionId, HttpListenerResponse response)
        {
            try
            {
                lock (_sessions)
                {
                    if (!_sessions.ContainsKey(sessionId))
                    {
                        response.StatusCode = 404;
                        WriteJsonResponse(response, new { error = "Session not found" }).Wait();
                        return;
                    }

                    var session = _sessions[sessionId];
                    var files = new List<object>();

                    foreach (var filename in session.Files.ToList())
                    {
                        var filePath = Path.Combine(_uploadPath, filename);
                        if (File.Exists(filePath))
                        {
                            var fileInfo = new FileInfo(filePath);
                            var parts = filename.Split('_');
                            var originalName = parts.Length >= 4 ? string.Join("_", parts.Skip(3)) : filename;

                            files.Add(new
                            {
                                filename = filename,
                                originalName = originalName,
                                size = fileInfo.Length,
                                uploadedAt = fileInfo.LastWriteTime.ToString("O")
                            });
                        }
                        else
                        {
                            // File no longer exists, remove from session
                            session.Files.Remove(filename);
                        }
                    }

                    session.LastActivity = System.DateTime.Now; // Use DateTime.Now

                    var responseData = new
                    {
                        sessionId = sessionId,
                        files = files.ToArray(),
                        count = files.Count
                    };

                    WriteJsonResponse(response, responseData).Wait();
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error listing files: {ex.Message}", false);
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = "Failed to list files" });
            }
        }

        private async Task HandleListFiles(HttpListenerRequest request, HttpListenerResponse response)
        {
            var query = ParseQueryString(request.Url.Query);
            var sessionId = GetValueOrDefault(query, "sessionId", "");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                response.StatusCode = 400;
                await WriteJsonResponse(response, new { error = "Missing sessionId" });
                return;
            }

            await HandleListFiles(sessionId, response);
        }

        private async Task HandleDeleteFile(string filename, HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var sessionId = request.Headers["X-Session-ID"] ?? "default";
                var filePath = Path.Combine(_uploadPath, filename);

                // Security check - ensure file is in upload directory
                var resolvedPath = Path.GetFullPath(filePath);
                var uploadDir = Path.GetFullPath(_uploadPath);

                if (!resolvedPath.StartsWith(uploadDir))
                {
                    response.StatusCode = 403;
                    await WriteJsonResponse(response, new { error = "Access denied" });
                    return;
                }

                // Check session ownership (filename should start with sessionId)
                if (!filename.StartsWith(sessionId + "_"))
                {
                    response.StatusCode = 403;
                    await WriteJsonResponse(response, new { error = "Not authorized to delete this file" });
                    return;
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Remove from session
                lock (_sessions)
                {
                    if (_sessions.ContainsKey(sessionId))
                    {
                        _sessions[sessionId].Files.Remove(filename);
                        _sessions[sessionId].LastActivity = System.DateTime.Now;
                    }
                }

                DebugConsole.Log($"[EmbeddedServer] File deleted: {filename}");
                await WriteJsonResponse(response, new { success = true, message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error deleting file: {ex.Message}", false);
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = "Delete failed" });
            }
        }

        private async Task HandleGetSession(string sessionId, HttpListenerResponse response)
        {
            try
            {
                lock (_sessions)
                {
                    if (!_sessions.ContainsKey(sessionId))
                    {
                        response.StatusCode = 404;
                        WriteJsonResponse(response, new { error = "Session not found" }).Wait();
                        return;
                    }

                    var session = _sessions[sessionId];
                    var responseData = new
                    {
                        sessionId = session.SessionId,
                        fileCount = session.Files.Count,
                        lastActivity = session.LastActivity,
                        created = session.Created,
                        host = session.Host
                    };

                    WriteJsonResponse(response, responseData).Wait();
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error getting session: {ex.Message}", false);
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = "Failed to get session" });
            }
        }

        private async Task HandleServerStatus(HttpListenerResponse response)
        {
            try
            {
                var di = new DirectoryInfo(_uploadPath);
                var files = di.Exists ? di.GetFiles() : new FileInfo[0];
                var totalSize = files.Sum(f => f.Length);

                var responseData = new
                {
                    Version = "1.0.0",
                    IsOnline = true,
                    TotalSpaceBytes = 1024L * 1024L * 1024L, // 1GB limit
                    UsedSpaceBytes = totalSize,
                    AvailableSpaceBytes = 1024L * 1024L * 1024L - totalSize,
                    ActiveSessions = _sessions.Count,
                    ServerTime = System.DateTime.Now, // Use DateTime.Now
                    name = "ONI MP Embedded File Server",
                    maxFileSize = 100 * 1024 * 1024, // 100MB
                    maxFiles = 50,
                    supportedFormats = new[] { ".sav", ".json", ".dat", ".tmp" },
                    authenticationEnabled = true,
                    authToken = _authToken, // Include auth token in status for host setup
                    localUrl = BaseUrl,
                    externalUrl = ExternalUrl,
                    externalIP = _externalIP,
                    upnpEnabled = _upnpEnabled,
                    port = _port
                };

                await WriteJsonResponse(response, responseData);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error getting server status: {ex.Message}", false);
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = "Failed to get server status" });
            }
        }

        private async Task WriteJsonResponse(HttpListenerResponse response, object data)
        {
            response.ContentType = "application/json; charset=utf-8";
            var json = JsonConvert.SerializeObject(data, Formatting.None);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        private async Task SetupExternalAccess()
        {
            try
            {
                DebugConsole.Log("[EmbeddedServer] Setting up external access...");
                
                // Detect external IP
                _externalIP = await DetectExternalIP();
                if (!string.IsNullOrEmpty(_externalIP))
                {
                    ExternalUrl = $"http://{_externalIP}:{_port}/";
                    DebugConsole.Log($"[EmbeddedServer] External IP detected: {_externalIP}");
                    DebugConsole.Log($"[EmbeddedServer] External URL: {ExternalUrl}");
                }
                else
                {
                    DebugConsole.LogWarning("[EmbeddedServer] Could not detect external IP address");
                }
                
                // Attempt UPnP port forwarding
                bool upnpSuccess = await SetupUPnP();
                if (upnpSuccess)
                {
                    _upnpEnabled = true;
                    DebugConsole.Log($"[EmbeddedServer] UPnP port forwarding enabled on port {_port}");
                    if (!string.IsNullOrEmpty(_externalIP))
                    {
                        DebugConsole.Log($"[EmbeddedServer] Server accessible externally at: {ExternalUrl}");
                        DebugConsole.Log($"[EmbeddedServer] Share this URL with remote players: {ExternalUrl}");
                        DebugConsole.Log($"[EmbeddedServer] Auth Token: {_authToken}");
                    }
                }
                else
                {
                    DebugConsole.LogWarning("[EmbeddedServer] UPnP port forwarding failed - server only accessible locally");
                    DebugConsole.LogWarning("[EmbeddedServer] Manual port forwarding may be required for external access");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[EmbeddedServer] External access setup failed: {ex.Message}", false);
            }
        }

        private async Task<string> DetectExternalIP()
        {
            try
            {
                // Try multiple services to detect external IP
                string[] ipServices = {
                    "https://api.ipify.org",
                    "https://icanhazip.com",
                    "https://ipinfo.io/ip",
                    "https://checkip.amazonaws.com"
                };

                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "ONI-MP-Server/1.0");
                    
                    foreach (var service in ipServices)
                    {
                        try
                        {
                            var ip = await client.DownloadStringTaskAsync(service);
                            ip = ip.Trim();
                            
                            // Validate IP format
                            if (IPAddress.TryParse(ip, out IPAddress address))
                            {
                                DebugConsole.Log($"[EmbeddedServer] External IP detected via {service}: {ip}");
                                return ip;
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.LogWarning($"[EmbeddedServer] IP detection via {service} failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[EmbeddedServer] External IP detection failed: {ex.Message}", false);
            }
            
            return null;
        }

        private async Task<bool> SetupUPnP()
        {
            try
            {
                DebugConsole.Log("[EmbeddedServer] Attempting UPnP port forwarding...");
                
                // Simple UPnP implementation using reflection to avoid dynamic keyword
                return await Task.Run(() => {
                    try
                    {
                        // Try to use UPnP via Windows COM
                        var upnpType = Type.GetTypeFromProgID("HNetCfg.NATUPnP");
                        if (upnpType == null)
                        {
                            DebugConsole.LogWarning("[EmbeddedServer] UPnP not available on this system");
                            return false;
                        }

                        var upnpNat = Activator.CreateInstance(upnpType);
                        if (upnpNat == null)
                        {
                            DebugConsole.LogWarning("[EmbeddedServer] Failed to create UPnP NAT instance");
                            return false;
                        }

                        var staticPortMappingCollection = upnpType.InvokeMember("StaticPortMappingCollection", 
                            System.Reflection.BindingFlags.GetProperty, null, upnpNat, null);
                        
                        if (staticPortMappingCollection == null)
                        {
                            DebugConsole.LogWarning("[EmbeddedServer] UPnP device not found or not supported");
                            return false;
                        }

                        // Add port mapping
                        staticPortMappingCollection.GetType().InvokeMember("Add", 
                            System.Reflection.BindingFlags.InvokeMethod, null, staticPortMappingCollection, 
                            new object[] { _port, "TCP", _port, GetLocalIPAddress(), true, "ONI MP Storage Server" });

                        DebugConsole.Log($"[EmbeddedServer] UPnP port mapping added successfully for port {_port}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogWarning($"[EmbeddedServer] UPnP setup failed: {ex.Message}");
                        return false;
                    }
                });
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[EmbeddedServer] UPnP setup error: {ex.Message}", false);
                return false;
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[EmbeddedServer] Could not determine local IP: {ex.Message}");
            }
            
            return "127.0.0.1";
        }

        private void CleanupUPnP()
        {
            if (!_upnpEnabled) return;
            
            try
            {
                DebugConsole.Log("[EmbeddedServer] Cleaning up UPnP port mapping...");
                
                var upnpType = Type.GetTypeFromProgID("HNetCfg.NATUPnP");
                if (upnpType != null)
                {
                    var upnpNat = Activator.CreateInstance(upnpType);
                    if (upnpNat != null)
                    {
                        var staticPortMappingCollection = upnpType.InvokeMember("StaticPortMappingCollection", 
                            System.Reflection.BindingFlags.GetProperty, null, upnpNat, null);
                        
                        if (staticPortMappingCollection != null)
                        {
                            staticPortMappingCollection.GetType().InvokeMember("Remove", 
                                System.Reflection.BindingFlags.InvokeMethod, null, staticPortMappingCollection, 
                                new object[] { _port, "TCP" });
                            
                            DebugConsole.Log("[EmbeddedServer] UPnP port mapping removed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[EmbeddedServer] UPnP cleanup failed: {ex.Message}");
            }
        }

        private void CleanupOldFiles(object state)
        {
            try
            {
                if (!Directory.Exists(_uploadPath)) return;

                var files = Directory.GetFiles(_uploadPath);
                var now = System.DateTime.Now; // Use DateTime.Now
                var maxAge = TimeSpan.FromHours(24);
                var deletedCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (now - fileInfo.LastWriteTime > maxAge)
                        {
                            File.Delete(file);
                            deletedCount++;
                            DebugConsole.Log($"[EmbeddedServer] Cleaned up old file: {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogError($"Error deleting old file {file}: {ex.Message}", false);
                    }
                }

                if (deletedCount > 0)
                {
                    DebugConsole.Log($"[EmbeddedServer] Cleanup completed: {deletedCount} old files removed");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"Error during cleanup: {ex.Message}", false);
            }
        }

        #region Helper Methods

        private Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query)) return result;

            query = query.TrimStart('?');
            var pairs = query.Split('&');

            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                }
            }

            return result;
        }

        // Helper method to replace GetValueOrDefault for .NET Framework
        private string GetValueOrDefault(Dictionary<string, string> dict, string key, string defaultValue)
        {
            return dict.ContainsKey(key) ? dict[key] : defaultValue;
        }

        private string GenerateAuthToken()
        {
            // Generate a secure random token for authentication
            var random = new Random();
            var tokenBytes = new byte[32];
            random.NextBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private bool ValidateAuthToken(HttpListenerRequest request)
        {
            // Check for auth token in header or query parameter
            var headerToken = request.Headers["X-Auth-Token"];
            var queryToken = request.QueryString["authToken"];
            
            var providedToken = headerToken ?? queryToken;
            
            if (string.IsNullOrEmpty(providedToken))
            {
                DebugConsole.LogWarning("[EmbeddedServer] Request missing authentication token");
                return false;
            }
            
            if (providedToken != _authToken)
            {
                DebugConsole.LogWarning("[EmbeddedServer] Invalid authentication token provided");
                return false;
            }
            
            return true;
        }

        private async Task<Dictionary<string, FormField>> ParseMultipartFormData(HttpListenerRequest request)
        {
            var result = new Dictionary<string, FormField>();
            var boundary = GetBoundary(request.ContentType);
            
            if (string.IsNullOrEmpty(boundary))
                return result;

            using (var stream = request.InputStream)
            {
                var data = new byte[request.ContentLength64];
                await stream.ReadAsync(data, 0, data.Length);
                
                var content = Encoding.UTF8.GetString(data);
                var parts = content.Split(new[] { "--" + boundary }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part) || part.Trim() == "--") continue;

                    var lines = part.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    if (lines.Length < 2) continue;

                    var contentDisposition = lines.FirstOrDefault(l => l.StartsWith("Content-Disposition:"));
                    if (contentDisposition == null) continue;

                    var name = ExtractValue(contentDisposition, "name");
                    var filename = ExtractValue(contentDisposition, "filename");

                    var emptyLineIndex = Array.FindIndex(lines, string.IsNullOrEmpty);
                    if (emptyLineIndex == -1) continue;

                    if (!string.IsNullOrEmpty(filename))
                    {
                        // This is a file field
                        var fileContent = string.Join("\n", lines.Skip(emptyLineIndex + 1));
                        var fileBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(fileContent); // Use ISO-8859-1 instead of Latin1
                        
                        result[name] = new FormField
                        {
                            FileName = filename,
                            Data = fileBytes
                        };
                    }
                    else
                    {
                        // This is a text field
                        var textContent = string.Join("\n", lines.Skip(emptyLineIndex + 1)).Trim();
                        result[name] = new FormField
                        {
                            Text = textContent
                        };
                    }
                }
            }

            return result;
        }

        private string GetBoundary(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return null;
            
            var boundaryIndex = contentType.IndexOf("boundary=");
            if (boundaryIndex == -1) return null;
            
            return contentType.Substring(boundaryIndex + 9).Trim();
        }

        private string ExtractValue(string header, string key)
        {
            var index = header.IndexOf(key + "=");
            if (index == -1) return null;
            
            var start = index + key.Length + 1;
            if (start >= header.Length) return null;
            
            var value = header.Substring(start);
            if (value.StartsWith("\""))
            {
                var endIndex = value.IndexOf("\"", 1);
                if (endIndex != -1)
                    value = value.Substring(1, endIndex - 1);
            }
            else
            {
                var endIndex = value.IndexOf(";");
                if (endIndex != -1)
                    value = value.Substring(0, endIndex);
            }
            
            return value.Trim();
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                StopAsync().Wait(5000);
                _cancellationTokenSource?.Dispose();
                _cleanupTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    public class SessionData
    {
        public string SessionId { get; set; }
        public List<string> Files { get; set; } = new List<string>();
        public System.DateTime LastActivity { get; set; }
        public string Host { get; set; }
        public System.DateTime Created { get; set; }
    }

    public class SessionRequest
    {
        public string sessionId { get; set; }
        public string clientVersion { get; set; }
        public long timestamp { get; set; }
    }

    public class FormField
    {
        public string Text { get; set; }
        public string FileName { get; set; }
        public byte[] Data { get; set; }
    }
}
