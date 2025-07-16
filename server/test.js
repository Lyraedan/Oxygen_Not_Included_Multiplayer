const http = require('http');
const fs = require('fs');
const path = require('path');

const serverUrl = 'http://localhost:3000';

console.log('Testing ONI MP File Server...');

// Test health endpoint
function testHealth() {
    return new Promise((resolve, reject) => {
        const req = http.get(`${serverUrl}/health`, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                if (res.statusCode === 200) {
                    console.log('✓ Health check passed');
                    resolve(JSON.parse(data));
                } else {
                    reject(`Health check failed: ${res.statusCode}`);
                }
            });
        });
        
        req.on('error', (err) => {
            reject(`Health check error: ${err.message}`);
        });
    });
}

// Test server info
function testInfo() {
    return new Promise((resolve, reject) => {
        const req = http.get(`${serverUrl}/info`, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                if (res.statusCode === 200) {
                    console.log('✓ Server info retrieved');
                    resolve(JSON.parse(data));
                } else {
                    reject(`Info request failed: ${res.statusCode}`);
                }
            });
        });
        
        req.on('error', (err) => {
            reject(`Info request error: ${err.message}`);
        });
    });
}

// Test session creation
function testSession() {
    return new Promise((resolve, reject) => {
        const sessionData = JSON.stringify({
            sessionId: 'test-session-123',
            host: 'test-host'
        });
        
        const options = {
            hostname: 'localhost',
            port: 3000,
            path: '/session',
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(sessionData)
            }
        };
        
        const req = http.request(options, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                if (res.statusCode === 200) {
                    console.log('✓ Session creation successful');
                    resolve(JSON.parse(data));
                } else {
                    reject(`Session creation failed: ${res.statusCode}`);
                }
            });
        });
        
        req.on('error', (err) => {
            reject(`Session creation error: ${err.message}`);
        });
        
        req.write(sessionData);
        req.end();
    });
}

// Run tests
async function runTests() {
    try {
        console.log('Starting tests...\n');
        
        const health = await testHealth();
        console.log(`Server status: ${health.status}, uptime: ${health.uptime}s\n`);
        
        const info = await testInfo();
        console.log(`Server: ${info.name} v${info.version}`);
        console.log(`Max file size: ${info.maxFileSize} bytes\n`);
        
        const session = await testSession();
        console.log(`Session created: ${session.sessionId}\n`);
        
        console.log('🎉 All tests passed! Server is working correctly.');
        
    } catch (error) {
        console.error('❌ Test failed:', error);
        console.log('\nMake sure the server is running with: npm start');
        process.exit(1);
    }
}

runTests();
