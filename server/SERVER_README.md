# ONI MP File Server

A dedicated Node.js file server for hosting Oxygen Not Included Multiplayer save files. This allows players to share save files without relying on Steam Cloud (which is user-specific) or Google Drive setup.

## Features

- **Session-based file management** - Multiple game sessions can run simultaneously
- **Multi-user support** - Perfect for multiplayer games where different Steam users need access
- **File size limits** - Configurable maximum file size (default: 100MB)
- **Automatic cleanup** - Old files are automatically removed after 24 hours
- **Security** - Optional authentication token support
- **CORS support** - Works with game clients from different origins
- **Logging** - Comprehensive logging for monitoring and debugging

## Quick Start

### Prerequisites

- [Node.js](https://nodejs.org/) version 16 or higher
- 500MB+ disk space for file storage

### Installation

1. **Navigate to the server directory:**
   ```bash
   cd server
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start the server:**
   
   **Windows:**
   ```cmd
   start-server.bat
   ```
   
   **Linux/macOS:**
   ```bash
   chmod +x start-server.sh
   ./start-server.sh
   ```
   
   **Or manually:**
   ```bash
   npm start
   ```

4. **Server will start on port 3000** (http://localhost:3000)

## Configuration

### Environment Variables

Create a `.env` file in the server directory or set these environment variables:

```bash
# Server port (default: 3000)
PORT=3000

# Upload directory (default: ./uploads)
UPLOAD_PATH=./uploads

# Maximum file size in bytes (default: 100MB)
MAX_FILE_SIZE=104857600

# Maximum number of files per session (default: 50)
MAX_FILES=50

# Optional authentication token
AUTH_TOKEN=your-secret-token-here

# CORS origins (default: *)
CORS_ORIGIN=*

# Log level (default: info)
LOG_LEVEL=info
```

### Game Configuration

In your ONI MP mod configuration (`multiplayer_settings.json`):

```json
{
  "Host": {
    "CloudStorage": {
      "Provider": "HttpCloud",
      "HttpCloud": {
        "HttpServerUrl": "http://your-server-ip:3000",
        "SessionId": "",
        "AuthToken": ""
      }
    }
  }
}
```

**Configuration Options:**

- **HttpServerUrl**: The URL where your file server is running
  - Local: `http://localhost:3000`
  - Network: `http://192.168.1.100:3000` (replace with your server IP)
  - Internet: `http://your-domain.com:3000`

- **SessionId**: Leave empty for auto-generation, or set a custom session ID for persistent sessions

- **AuthToken**: Optional authentication token (must match server's AUTH_TOKEN)

## API Endpoints

### Health Check
```
GET /health
```
Returns server status and uptime.

### Server Info
```
GET /info
```
Returns server configuration and supported file formats.

### Session Management
```
POST /session
Content-Type: application/json

{
  "sessionId": "your-session-id",
  "host": "player-name"
}
```

### File Upload
```
POST /upload
Content-Type: multipart/form-data
X-Session-ID: your-session-id

Form data: file (binary)
```

### File Download
```
GET /download/:filename
X-Session-ID: your-session-id
```

### List Files
```
GET /files/:sessionId
```

### Delete File
```
DELETE /delete/:filename
X-Session-ID: your-session-id
```

## Deployment

### Local Network

1. **Find your computer's IP address:**
   ```bash
   # Windows
   ipconfig
   
   # Linux/macOS
   ifconfig
   ```

2. **Start the server and use your IP in game configuration:**
   ```
   http://192.168.1.100:3000
   ```

3. **Make sure port 3000 is open in your firewall**

### Internet Deployment

For internet-accessible servers, consider:

1. **Use a VPS or cloud provider** (AWS, DigitalOcean, etc.)
2. **Set up a reverse proxy** (nginx, Apache) with SSL
3. **Use a process manager** (PM2) for production
4. **Configure firewall and security groups**

Example PM2 setup:
```bash
npm install -g pm2
pm2 start server.js --name "oni-mp-server"
pm2 save
pm2 startup
```

## Security Considerations

- **Set AUTH_TOKEN** for public servers
- **Use HTTPS** in production (requires reverse proxy)
- **Limit CORS_ORIGIN** to specific domains
- **Monitor disk usage** (automatic cleanup helps)
- **Regular backups** of important save files
- **Firewall configuration** to restrict access

## Troubleshooting

### Server Won't Start
- Check if Node.js is installed: `node --version`
- Check if port 3000 is available
- Look at the server logs for error messages

### Game Can't Connect
- Verify server URL in game configuration
- Check firewall settings
- Test server with: `curl http://localhost:3000/health`
- Check server logs for connection attempts

### Files Not Uploading
- Check disk space on server
- Verify file size limits
- Check authentication token (if used)
- Look at both server and game logs

### Common Error Messages

**"EADDRINUSE: address already in use"**
- Port 3000 is already used by another application
- Change PORT in configuration or stop the conflicting application

**"Connection refused"**
- Server is not running
- Wrong IP address or port in game configuration
- Firewall blocking connections

**"Unauthorized"**
- AUTH_TOKEN mismatch between server and game
- Check authentication configuration

## File Format Support

The server accepts these file types:
- `.sav` - Save files
- `.json` - Configuration files
- `.dat` - Data files
- `.tmp` - Temporary files

## Monitoring

Server logs include:
- Connection attempts
- File uploads/downloads
- Errors and warnings
- Session activity

Log files are saved to `server.log` in the server directory.

## Support

For issues and questions:
1. Check this documentation
2. Look at server logs
3. Test with curl or browser
4. Check the main project issues on GitHub
