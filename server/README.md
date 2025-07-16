# ONI MP File Server

A dedicated Node.js server for hosting save files in ONI Multiplayer games. This allows players to share save files across different Steam accounts, overcoming the limitation of Steam Cloud which only works for individual users.

## Features

- **Cross-user save sharing**: Multiple players can access the same save files
- **Session-based organization**: Files are organized by multiplayer sessions/lobbies
- **Large file support**: No 10MB Steam Cloud limitation
- **Simple HTTP API**: Easy integration with the ONI MP mod
- **Security**: Optional authentication and CORS protection
- **Automatic cleanup**: Old files are automatically removed
- **Logging**: Comprehensive logging for debugging

## Quick Start

### Prerequisites

- **Node.js 16+**: Download from [nodejs.org](https://nodejs.org/)
- **Network access**: Ensure the server port (default 3000) is accessible to players

### Installation

1. **Navigate to the server directory**:
   ```bash
   cd server
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Start the server**:
   
   **Windows**:
   ```cmd
   start-server.bat
   ```
   
   **Linux/Mac**:
   ```bash
   chmod +x start-server.sh
   ./start-server.sh
   ```
   
   **Or manually**:
   ```bash
   npm start
   ```

4. **Verify the server is running**:
   - Open your browser to `http://localhost:3000/health`
   - You should see a JSON response indicating the server is healthy

## Configuration

### Environment Variables

Create a `.env` file in the server directory (copy from `.env.example`):

```env
# Server Configuration
PORT=3000
UPLOAD_PATH=./uploads
MAX_FILE_SIZE=104857600  # 100MB
MAX_FILES=50
LOG_LEVEL=info

# Security (Optional but recommended)
AUTH_TOKEN=your-secret-token-here

# CORS Configuration
CORS_ORIGIN=*
```

### Key Settings

- **PORT**: Server port (default: 3000)
- **UPLOAD_PATH**: Directory to store uploaded files
- **MAX_FILE_SIZE**: Maximum file size in bytes (default: 100MB)
- **AUTH_TOKEN**: Optional authentication token for security
- **CORS_ORIGIN**: Allowed origins for CORS (use `*` for development)

## Game Configuration

Update your `multiplayer_settings.json` in the ONI MP mod:

```json
{
  "Host": {
    "CloudStorage": {
      "Provider": "HttpCloud",
      "HttpCloud": {
        "HttpServerUrl": "http://your-server-ip:3000",
        "SessionId": "",
        "AuthToken": "your-secret-token-here"
      }
    }
  }
}
```

### Configuration Options

- **HttpServerUrl**: Your server URL (e.g., `http://192.168.1.100:3000`)
- **SessionId**: Leave empty for auto-generation based on Steam lobby
- **AuthToken**: Must match the server's AUTH_TOKEN if authentication is enabled

## Network Setup

### Local Network (LAN)

1. **Find your server's IP address**:
   - Windows: `ipconfig`
   - Linux/Mac: `ip addr` or `ifconfig`

2. **Use the IP in game config**:
   ```json
   "HttpServerUrl": "http://192.168.1.100:3000"
   ```

3. **Ensure firewall allows connections** on port 3000

### Internet/Public Server

⚠️ **Security Warning**: Only expose to the internet with proper security measures!

1. **Set a strong authentication token**:
   ```env
   AUTH_TOKEN=your-very-secure-random-token-here
   ```

2. **Configure your router/firewall** to forward port 3000 to your server

3. **Use your public IP or domain**:
   ```json
   "HttpServerUrl": "http://your-public-ip:3000"
   ```

4. **Consider using HTTPS** with a reverse proxy (nginx, Apache) for production

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
Returns server capabilities and limits.

### Session Management
```
POST /session
Body: { "sessionId": "your-session", "host": "hostname" }
```
Creates or joins a multiplayer session.

### File Operations
```
POST /upload (with file)
GET /download/:filename
GET /files/:sessionId
DELETE /delete/:filename
```

## Troubleshooting

### Server Won't Start

1. **Check Node.js installation**:
   ```bash
   node --version
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Check port availability**:
   ```bash
   # Linux/Mac
   netstat -tlnp | grep 3000
   
   # Windows
   netstat -an | findstr 3000
   ```

### Game Can't Connect

1. **Verify server is running**: Check `http://localhost:3000/health`

2. **Check game configuration**: Ensure `HttpServerUrl` is correct

3. **Test network connectivity**:
   ```bash
   # From game client machine
   curl http://server-ip:3000/health
   ```

4. **Check firewall settings**: Ensure port 3000 is open

### Files Not Uploading

1. **Check file size**: Ensure files are under the size limit (100MB default)

2. **Check server logs**: Look for error messages in the console

3. **Verify authentication**: Ensure `AuthToken` matches between server and game

4. **Check disk space**: Ensure server has enough storage space

## Security Considerations

### Authentication
- Always use `AUTH_TOKEN` for internet-facing servers
- Use long, random tokens (32+ characters)
- Rotate tokens regularly

### Network Security
- Use HTTPS with SSL certificates for production
- Configure CORS properly (avoid `*` in production)
- Consider VPN for team access instead of public exposure

### File Security
- Regular cleanup of old files (automatic)
- Monitor disk usage
- Consider file type restrictions

## Development

### Running in Development Mode
```bash
npm run dev
```
Uses nodemon for automatic restarts on code changes.

### Logs
- Console output for real-time monitoring
- `server.log` file for persistent logging
- Configurable log levels: `error`, `warn`, `info`, `debug`

## Support

For issues and support:
1. Check the [troubleshooting section](#troubleshooting)
2. Review server logs for error messages
3. Open an issue on the [GitHub repository](https://github.com/Lyraedan/Oxygen_Not_Included_Multiplayer)
4. Join the [Discord community](https://discord.gg/jpxveK6mmY)
