# StorageServer Client

The StorageServer client provides HTTP-based file sharing for the ONI Multiplayer mod. This allows players to use a self-hosted server instead of relying on Google Drive or other third-party services.

## Components

### 1. StorageServerClient
Low-level HTTP client that handles direct communication with the storage server.

**Key Features:**
- Connection testing and health checks
- Session management with auto-generated IDs
- File upload/download with retry logic
- File listing and server status queries
- Proper resource disposal and error handling

### 2. StorageServerUtils
High-level utility class that simplifies common operations and manages configuration.

**Key Features:**
- Automatic initialization from configuration
- Session persistence across game sessions
- Simplified API for common operations
- Built-in error handling and logging

### 3. StorageServerProvider
Integration with the SharedAccessStorageManager system for seamless provider switching.

## Configuration

The StorageServer is configured in `multiplayer_settings.json`:

```json
{
  "Host": {
    "CloudStorage": {
      "Provider": "StorageServer",
      "StorageServer": {
        "HttpServerUrl": "http://localhost:3000",
        "SessionId": "",
        "AuthToken": ""
      }
    }
  }
}
```

### Configuration Properties:
- **HttpServerUrl**: The base URL of your storage server
- **SessionId**: Auto-generated session ID (leave empty for auto-generation)
- **AuthToken**: Optional authentication token for secured servers

## API Endpoints

The client expects the storage server to implement these HTTP endpoints:

### Health Check
- **GET** `/health` - Test server connectivity
- **Response**: `200 OK` if server is healthy

### Session Management
- **POST** `/session` - Create a new session
- **Body**: `{"sessionId": "user_123", "clientVersion": "1.0", "timestamp": 1234567890}`
- **Response**: `{"sessionId": "user_123", "status": "created"}`

### File Operations
- **POST** `/upload` - Upload a file
- **Form Data**: 
  - `sessionId`: Session identifier
  - `fileName`: Target filename
  - `file`: File content (multipart)

- **GET** `/download?sessionId={id}&fileName={name}` - Download a file
- **Response**: File content as binary data

- **GET** `/files?sessionId={id}` - List files in session
- **Response**: `{"files": ["file1.sav", "file2.sav"], "count": 2}`

### Server Information
- **GET** `/status` - Get server status and quota info
- **Response**: 
```json
{
  "version": "1.0",
  "isOnline": true,
  "totalSpaceBytes": 1073741824,
  "usedSpaceBytes": 52428800,
  "availableSpaceBytes": 1021313024,
  "activeSessions": 5,
  "serverTime": "2025-07-17T23:30:00Z"
}
```

## Usage Examples

### Direct Client Usage
```csharp
// Create client
var client = new StorageServerClient("http://localhost:3000", "optional-auth-token");

// Test connection
bool connected = await client.TestConnectionAsync();

// Create session
string sessionId = await client.CreateSessionAsync();

// Upload file
bool uploaded = await client.UploadFileAsync("save.sav", "multiplayer_save.sav", sessionId);

// Download file
bool downloaded = await client.DownloadFileAsync("multiplayer_save.sav", "local_save.sav", sessionId);

// List files
var files = await client.ListFilesAsync(sessionId);

// Get server status
var status = await client.GetServerStatusAsync();

// Cleanup
client.Dispose();
```

### High-Level Utils Usage
```csharp
// Initialize using configuration
bool initialized = await StorageServerUtils.InitializeAsync();

if (initialized)
{
    // Upload file (remote name auto-generated)
    await StorageServerUtils.UploadFileAsync("local_save.sav");
    
    // Download file
    await StorageServerUtils.DownloadFileAsync("multiplayer_save.sav", "downloaded_save.sav");
    
    // List available files
    var files = await StorageServerUtils.ListFilesAsync();
    
    // Get server status
    var status = await StorageServerUtils.GetServerStatusAsync();
    
    // Check current session
    string sessionId = StorageServerUtils.GetCurrentSessionId();
}

// Cleanup when done
StorageServerUtils.Cleanup();
```

### Integration with SharedAccessStorageManager
The StorageServerProvider automatically integrates with the existing storage system:

```csharp
// Switch to StorageServer provider
SharedAccessStorageManager.Instance.SwitchProvider("StorageServer");

// Use standard interface
SharedAccessStorageManager.Instance.UploadFile("save.sav", "multiplayer_save.sav");
SharedAccessStorageManager.Instance.DownloadFile("multiplayer_save.sav", "local_save.sav");
```

## Error Handling

The client includes comprehensive error handling:
- **Connection failures**: Automatic retry with exponential backoff
- **Authentication errors**: Clear logging of auth token issues  
- **File not found**: Proper error reporting for missing files
- **Server errors**: Detailed logging of HTTP error responses
- **Network timeouts**: Configurable timeout with retry logic

## Security Considerations

- **Authentication**: Use AuthToken for secured servers
- **HTTPS**: Use HTTPS URLs in production (`https://your-server.com`)
- **Session management**: Sessions are tied to Steam IDs when available
- **File validation**: Server should validate file types and sizes
- **Rate limiting**: Server should implement rate limiting for uploads

## Server Implementation

You'll need to implement a compatible HTTP server that supports the required endpoints. Example technologies:
- **Node.js + Express**
- **Python + Flask/FastAPI** 
- **Go + Gin/Echo**
- **C# + ASP.NET Core**

The server should handle:
- Session-based file isolation
- File storage and retrieval
- Authentication (if using AuthToken)
- Cleanup of old sessions
- Storage quota management
