const express = require('express');
const multer = require('multer');
const cors = require('cors');
const helmet = require('helmet');
const compression = require('compression');
const winston = require('winston');
const path = require('path');
const fs = require('fs').promises;
const crypto = require('crypto');

// Configuration
const config = {
    port: process.env.PORT || 3000,
    uploadPath: process.env.UPLOAD_PATH || './uploads',
    maxFileSize: process.env.MAX_FILE_SIZE || 100 * 1024 * 1024, // 100MB
    maxFiles: process.env.MAX_FILES || 50,
    authToken: process.env.AUTH_TOKEN || null, // Optional authentication
    corsOrigin: process.env.CORS_ORIGIN || '*',
    logLevel: process.env.LOG_LEVEL || 'info'
};

// Setup logging
const logger = winston.createLogger({
    level: config.logLevel,
    format: winston.format.combine(
        winston.format.timestamp(),
        winston.format.printf(({ timestamp, level, message, ...meta }) => {
            return `${timestamp} [${level.toUpperCase()}]: ${message} ${Object.keys(meta).length ? JSON.stringify(meta) : ''}`;
        })
    ),
    transports: [
        new winston.transports.Console(),
        new winston.transports.File({ filename: 'server.log' })
    ]
});

const app = express();

// Security and middleware
app.use(helmet());
app.use(compression());
app.use(cors({
    origin: config.corsOrigin,
    methods: ['GET', 'POST', 'PUT', 'DELETE'],
    allowedHeaders: ['Content-Type', 'Authorization', 'X-Session-ID']
}));

app.use(express.json());

// Authentication middleware (optional)
const authenticate = (req, res, next) => {
    if (!config.authToken) {
        return next(); // No auth required
    }
    
    const token = req.headers.authorization?.replace('Bearer ', '');
    if (token !== config.authToken) {
        logger.warn(`Unauthorized access attempt from ${req.ip}`, { 
            path: req.path, 
            userAgent: req.get('User-Agent') 
        });
        return res.status(401).json({ error: 'Unauthorized' });
    }
    next();
};

// Session management for multiplayer
const sessions = new Map(); // sessionId -> { files: Set, lastActivity: Date, host: string }

// Ensure upload directory exists
async function ensureUploadDir() {
    try {
        await fs.access(config.uploadPath);
    } catch {
        await fs.mkdir(config.uploadPath, { recursive: true });
        logger.info(`Created upload directory: ${config.uploadPath}`);
    }
}

// File storage configuration
const storage = multer.diskStorage({
    destination: config.uploadPath,
    filename: (req, file, cb) => {
        const sessionId = req.headers['x-session-id'] || 'default';
        const timestamp = Date.now();
        const hash = crypto.createHash('md5').update(file.originalname + timestamp).digest('hex').substr(0, 8);
        const filename = `${sessionId}_${timestamp}_${hash}_${file.originalname}`;
        cb(null, filename);
    }
});

const upload = multer({
    storage,
    limits: {
        fileSize: config.maxFileSize,
        files: 1
    },
    fileFilter: (req, file, cb) => {
        // Accept save files and related game files
        const allowedExtensions = ['.sav', '.json', '.dat', '.tmp'];
        const ext = path.extname(file.originalname).toLowerCase();
        
        if (allowedExtensions.includes(ext)) {
            cb(null, true);
        } else {
            cb(new Error(`File type not allowed. Allowed types: ${allowedExtensions.join(', ')}`));
        }
    }
});

// Cleanup old files periodically
setInterval(async () => {
    try {
        const files = await fs.readdir(config.uploadPath);
        const now = Date.now();
        const maxAge = 24 * 60 * 60 * 1000; // 24 hours
        
        for (const file of files) {
            const filePath = path.join(config.uploadPath, file);
            const stats = await fs.stat(filePath);
            
            if (now - stats.mtime.getTime() > maxAge) {
                await fs.unlink(filePath);
                logger.info(`Cleaned up old file: ${file}`);
            }
        }
    } catch (error) {
        logger.error('Error during cleanup:', error);
    }
}, 60 * 60 * 1000); // Run every hour

// Routes

// Health check
app.get('/health', (req, res) => {
    res.json({ 
        status: 'healthy', 
        timestamp: new Date().toISOString(),
        uptime: process.uptime(),
        version: require('./package.json').version
    });
});

// Get server info
app.get('/info', (req, res) => {
    res.json({
        name: 'ONI MP File Server',
        version: require('./package.json').version,
        maxFileSize: config.maxFileSize,
        maxFiles: config.maxFiles,
        supportedFormats: ['.sav', '.json', '.dat', '.tmp']
    });
});

// Create or join session
app.post('/session', authenticate, async (req, res) => {
    try {
        const { sessionId, host } = req.body;
        
        if (!sessionId) {
            return res.status(400).json({ error: 'Session ID is required' });
        }
        
        if (!sessions.has(sessionId)) {
            sessions.set(sessionId, {
                files: new Set(),
                lastActivity: new Date(),
                host: host || req.ip,
                created: new Date()
            });
            logger.info(`Created new session: ${sessionId}`, { host });
        } else {
            sessions.get(sessionId).lastActivity = new Date();
        }
        
        res.json({ 
            sessionId, 
            status: 'active',
            files: Array.from(sessions.get(sessionId).files)
        });
    } catch (error) {
        logger.error('Error creating/joining session:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// Upload file
app.post('/upload', authenticate, upload.single('file'), async (req, res) => {
    try {
        if (!req.file) {
            return res.status(400).json({ error: 'No file provided' });
        }
        
        const sessionId = req.headers['x-session-id'] || 'default';
        const session = sessions.get(sessionId);
        
        if (!session) {
            return res.status(404).json({ error: 'Session not found. Create a session first.' });
        }
        
        // Update session
        session.files.add(req.file.filename);
        session.lastActivity = new Date();
        
        const fileInfo = {
            filename: req.file.filename,
            originalName: req.file.originalname,
            size: req.file.size,
            uploadedAt: new Date().toISOString(),
            sessionId: sessionId
        };
        
        logger.info(`File uploaded successfully`, fileInfo);
        
        res.json({
            success: true,
            file: fileInfo
        });
        
    } catch (error) {
        logger.error('Error uploading file:', error);
        res.status(500).json({ error: error.message || 'Upload failed' });
    }
});

// Download file
app.get('/download/:filename', authenticate, async (req, res) => {
    try {
        const filename = req.params.filename;
        const filePath = path.join(config.uploadPath, filename);
        
        // Security check - ensure file is in upload directory
        const resolvedPath = path.resolve(filePath);
        const uploadDir = path.resolve(config.uploadPath);
        
        if (!resolvedPath.startsWith(uploadDir)) {
            return res.status(403).json({ error: 'Access denied' });
        }
        
        // Check if file exists
        try {
            await fs.access(filePath);
        } catch {
            return res.status(404).json({ error: 'File not found' });
        }
        
        // Extract original filename for download
        const parts = filename.split('_');
        const originalName = parts.slice(3).join('_'); // Remove sessionId, timestamp, and hash
        
        logger.info(`File download requested: ${filename}`, { 
            ip: req.ip,
            userAgent: req.get('User-Agent')
        });
        
        res.download(filePath, originalName);
        
    } catch (error) {
        logger.error('Error downloading file:', error);
        res.status(500).json({ error: 'Download failed' });
    }
});

// List files for session
app.get('/files/:sessionId', authenticate, async (req, res) => {
    try {
        const sessionId = req.params.sessionId;
        const session = sessions.get(sessionId);
        
        if (!session) {
            return res.status(404).json({ error: 'Session not found' });
        }
        
        const files = [];
        for (const filename of session.files) {
            const filePath = path.join(config.uploadPath, filename);
            
            try {
                const stats = await fs.stat(filePath);
                const parts = filename.split('_');
                
                files.push({
                    filename,
                    originalName: parts.slice(3).join('_'),
                    size: stats.size,
                    uploadedAt: stats.mtime.toISOString()
                });
            } catch (error) {
                // File no longer exists, remove from session
                session.files.delete(filename);
            }
        }
        
        session.lastActivity = new Date();
        
        res.json({ sessionId, files });
        
    } catch (error) {
        logger.error('Error listing files:', error);
        res.status(500).json({ error: 'Failed to list files' });
    }
});

// Delete file
app.delete('/delete/:filename', authenticate, async (req, res) => {
    try {
        const filename = req.params.filename;
        const sessionId = req.headers['x-session-id'] || 'default';
        const filePath = path.join(config.uploadPath, filename);
        
        // Security check
        const resolvedPath = path.resolve(filePath);
        const uploadDir = path.resolve(config.uploadPath);
        
        if (!resolvedPath.startsWith(uploadDir)) {
            return res.status(403).json({ error: 'Access denied' });
        }
        
        // Check session ownership (filename should start with sessionId)
        if (!filename.startsWith(sessionId + '_')) {
            return res.status(403).json({ error: 'Not authorized to delete this file' });
        }
        
        await fs.unlink(filePath);
        
        // Remove from session
        const session = sessions.get(sessionId);
        if (session) {
            session.files.delete(filename);
            session.lastActivity = new Date();
        }
        
        logger.info(`File deleted: ${filename}`, { sessionId });
        
        res.json({ success: true, message: 'File deleted successfully' });
        
    } catch (error) {
        logger.error('Error deleting file:', error);
        res.status(500).json({ error: 'Delete failed' });
    }
});

// Get session info
app.get('/session/:sessionId', authenticate, (req, res) => {
    const sessionId = req.params.sessionId;
    const session = sessions.get(sessionId);
    
    if (!session) {
        return res.status(404).json({ error: 'Session not found' });
    }
    
    res.json({
        sessionId,
        fileCount: session.files.size,
        lastActivity: session.lastActivity,
        created: session.created,
        host: session.host
    });
});

// Error handling middleware
app.use((error, req, res, next) => {
    logger.error('Unhandled error:', error);
    
    if (error instanceof multer.MulterError) {
        if (error.code === 'LIMIT_FILE_SIZE') {
            return res.status(413).json({ error: 'File too large' });
        }
        if (error.code === 'LIMIT_FILE_COUNT') {
            return res.status(413).json({ error: 'Too many files' });
        }
    }
    
    res.status(500).json({ error: error.message || 'Internal server error' });
});

// 404 handler
app.use((req, res) => {
    res.status(404).json({ error: 'Endpoint not found' });
});

// Start server
async function startServer() {
    try {
        await ensureUploadDir();
        
        app.listen(config.port, () => {
            logger.info(`ONI MP File Server started on port ${config.port}`, {
                config: {
                    maxFileSize: config.maxFileSize,
                    maxFiles: config.maxFiles,
                    uploadPath: config.uploadPath,
                    authEnabled: !!config.authToken
                }
            });
        });
        
    } catch (error) {
        logger.error('Failed to start server:', error);
        process.exit(1);
    }
}

// Graceful shutdown
process.on('SIGTERM', () => {
    logger.info('Received SIGTERM, shutting down gracefully');
    process.exit(0);
});

process.on('SIGINT', () => {
    logger.info('Received SIGINT, shutting down gracefully');
    process.exit(0);
});

startServer();
