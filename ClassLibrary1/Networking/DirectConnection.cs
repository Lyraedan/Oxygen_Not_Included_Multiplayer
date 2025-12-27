using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ONI_MP.Networking
{
    public enum ConnectionMode
    {
        SteamP2P,
        DirectIP
    }

    public static class DirectConnection
    {
        public const int DEFAULT_PORT = 11000;

        private static TcpListener _server;
        private static TcpClient _client;
        private static NetworkStream _stream;
        private static Thread _listenThread;
        private static Thread _receiveThread;
        private static bool _isRunning;

        // 存储已连接的客户端（主机用）
        private static readonly ConcurrentDictionary<string, TcpClient> _connectedClients
            = new ConcurrentDictionary<string, TcpClient>();

        // 当前连接模式
        public static ConnectionMode Mode { get; set; } = ConnectionMode.SteamP2P;

        // 连接状态
        public static bool IsDirectConnected
        {
            get { return (_client != null && _client.Connected) || _connectedClients.Count > 0; }
        }

        public static bool IsServerRunning
        {
            get { return _isRunning && _server != null; }
        }

        public static bool IsClientConnected
        {
            get { return _isRunning && _client != null && _client.Connected; }
        }

        #region 主机端

        /// <summary>
        /// 作为主机启动直连服务器
        /// </summary>
        public static bool StartServer(int port)
        {
            if (_isRunning)
            {
                DebugConsole.LogWarning("[DirectConnection] Server already running");
                return false;
            }

            try
            {
                _server = new TcpListener(IPAddress.Any, port);
                _server.Start();
                _isRunning = true;

                _listenThread = new Thread(ListenForClients);
                _listenThread.IsBackground = true;
                _listenThread.Name = "DirectConnection_Listen";
                _listenThread.Start();

                Mode = ConnectionMode.DirectIP;
                MultiplayerSession.InSession = true;

                DebugConsole.Log("[DirectConnection] Server started on port " + port);
                DebugConsole.Log("[DirectConnection] Your IP: " + GetLocalIPAddress() + ":" + port);

                return true;
            }
            catch (Exception ex)
            {
                DebugConsole.LogError("[DirectConnection] Failed to start server: " + ex.Message);
                return false;
            }
        }

        public static bool StartServer()
        {
            return StartServer(DEFAULT_PORT);
        }

        private static void ListenForClients()
        {
            while (_isRunning)
            {
                try
                {
                    if (_server != null && _server.Pending())
                    {
                        TcpClient client = _server.AcceptTcpClient();

                        string clientId;
                        IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                        if (remoteEndPoint != null)
                        {
                            clientId = remoteEndPoint.ToString();
                        }
                        else
                        {
                            clientId = Guid.NewGuid().ToString();
                        }

                        _connectedClients[clientId] = client;
                        DebugConsole.Log("[DirectConnection] Client connected: " + clientId);

                        // 为每个客户端启动接收线程
                        ClientReceiveThreadData threadData = new ClientReceiveThreadData();
                        threadData.ClientId = clientId;
                        threadData.Client = client;

                        Thread clientThread = new Thread(ReceiveFromClientThread);
                        clientThread.IsBackground = true;
                        clientThread.Name = "DirectConnection_Client_" + clientId;
                        clientThread.Start(threadData);
                    }
                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        DebugConsole.LogError("[DirectConnection] Listen error: " + ex.Message);
                }
            }
        }

        private class ClientReceiveThreadData
        {
            public string ClientId;
            public TcpClient Client;
        }

        private static void ReceiveFromClientThread(object data)
        {
            ClientReceiveThreadData threadData = (ClientReceiveThreadData)data;
            ReceiveFromClient(threadData.ClientId, threadData.Client);
        }

        private static void ReceiveFromClient(string clientId, TcpClient client)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                byte[] lengthBuffer = new byte[4];

                while (_isRunning && client.Connected)
                {
                    try
                    {
                        if (stream.DataAvailable)
                        {
                            // 读取数据包长度
                            int bytesRead = stream.Read(lengthBuffer, 0, 4);
                            if (bytesRead == 0) break;

                            int packetLength = BitConverter.ToInt32(lengthBuffer, 0);

                            if (packetLength <= 0 || packetLength > 10 * 1024 * 1024)
                            {
                                DebugConsole.LogWarning("[DirectConnection] Invalid packet length: " + packetLength);
                                break;
                            }

                            // 读取数据包内容
                            byte[] packetData = new byte[packetLength];
                            int totalRead = 0;
                            while (totalRead < packetLength)
                            {
                                int read = stream.Read(packetData, totalRead, packetLength - totalRead);
                                if (read == 0) break;
                                totalRead = totalRead + read;
                            }

                            if (totalRead == packetLength)
                            {
                                try
                                {
                                    PacketHandler.HandleIncoming(packetData);
                                }
                                catch (Exception ex)
                                {
                                    DebugConsole.LogError("[DirectConnection] Packet handling error: " + ex.Message);
                                }
                            }
                        }
                        Thread.Sleep(1);
                    }
                    catch (Exception ex)
                    {
                        if (_isRunning)
                            DebugConsole.LogError("[DirectConnection] Receive error from " + clientId + ": " + ex.Message);
                        break;
                    }
                }
            }
            finally
            {
                TcpClient removed;
                _connectedClients.TryRemove(clientId, out removed);
                try { client.Close(); }
                catch { }
                DebugConsole.Log("[DirectConnection] Client disconnected: " + clientId);
            }
        }

        /// <summary>
        /// 停止直连服务器
        /// </summary>
        public static void StopServer()
        {
            _isRunning = false;

            foreach (TcpClient client in _connectedClients.Values)
            {
                try { client.Close(); }
                catch { }
            }
            _connectedClients.Clear();

            if (_server != null)
            {
                try { _server.Stop(); }
                catch { }
                _server = null;
            }

            Mode = ConnectionMode.SteamP2P;
            MultiplayerSession.InSession = false;

            DebugConsole.Log("[DirectConnection] Server stopped");
        }

        #endregion

        #region 客户端

        /// <summary>
        /// 作为客户端连接到主机
        /// </summary>
        public static bool Connect(string ip, int port)
        {
            if (_isRunning)
            {
                DebugConsole.LogWarning("[DirectConnection] Already connected");
                return false;
            }

            try
            {
                DebugConsole.Log("[DirectConnection] Connecting to " + ip + ":" + port + "...");

                _client = new TcpClient();
                _client.Connect(ip, port);
                _stream = _client.GetStream();
                _isRunning = true;

                _receiveThread = new Thread(ReceiveFromServer);
                _receiveThread.IsBackground = true;
                _receiveThread.Name = "DirectConnection_Receive";
                _receiveThread.Start();

                Mode = ConnectionMode.DirectIP;
                MultiplayerSession.InSession = true;

                GameClient.SetState(ClientState.Connected);

                DebugConsole.Log("[DirectConnection] Connected to " + ip + ":" + port);

                return true;
            }
            catch (Exception ex)
            {
                DebugConsole.LogError("[DirectConnection] Failed to connect: " + ex.Message);
                Disconnect();
                return false;
            }
        }

        public static bool Connect(string ip)
        {
            return Connect(ip, DEFAULT_PORT);
        }

        private static void ReceiveFromServer()
        {
            byte[] lengthBuffer = new byte[4];

            while (_isRunning && _client != null && _client.Connected)
            {
                try
                {
                    if (_stream != null && _stream.DataAvailable)
                    {
                        int bytesRead = _stream.Read(lengthBuffer, 0, 4);
                        if (bytesRead == 0) break;

                        int packetLength = BitConverter.ToInt32(lengthBuffer, 0);

                        if (packetLength <= 0 || packetLength > 10 * 1024 * 1024)
                        {
                            DebugConsole.LogWarning("[DirectConnection] Invalid packet length: " + packetLength);
                            break;
                        }

                        byte[] packetData = new byte[packetLength];
                        int totalRead = 0;
                        while (totalRead < packetLength)
                        {
                            int read = _stream.Read(packetData, totalRead, packetLength - totalRead);
                            if (read == 0) break;
                            totalRead = totalRead + read;
                        }

                        if (totalRead == packetLength)
                        {
                            try
                            {
                                PacketHandler.HandleIncoming(packetData);
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.LogError("[DirectConnection] Packet handling error: " + ex.Message);
                            }
                        }
                    }
                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        DebugConsole.LogError("[DirectConnection] Receive error: " + ex.Message);
                    break;
                }
            }

            Disconnect();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public static void Disconnect()
        {
            bool wasConnected = _isRunning;
            _isRunning = false;

            if (_stream != null)
            {
                try { _stream.Close(); }
                catch { }
                _stream = null;
            }

            if (_client != null)
            {
                try { _client.Close(); }
                catch { }
                _client = null;
            }

            Mode = ConnectionMode.SteamP2P;
            MultiplayerSession.InSession = false;
            GameClient.SetState(ClientState.Disconnected);

            if (wasConnected)
            {
                DebugConsole.Log("[DirectConnection] Disconnected");
            }
        }

        #endregion

        #region 发送数据

        /// <summary>
        /// 发送数据包（客户端 -> 主机）
        /// </summary>
        public static bool SendToServer(byte[] data)
        {
            if (_stream == null || _client == null || !_client.Connected)
            {
                DebugConsole.LogWarning("[DirectConnection] Cannot send: not connected to server");
                return false;
            }

            try
            {
                lock (_stream)
                {
                    byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
                    _stream.Write(lengthPrefix, 0, 4);
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                }
                return true;
            }
            catch (Exception ex)
            {
                DebugConsole.LogError("[DirectConnection] Send to server error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 发送数据包给指定客户端（主机用）
        /// </summary>
        public static bool SendToClient(string clientId, byte[] data)
        {
            TcpClient client;
            if (!_connectedClients.TryGetValue(clientId, out client) || client == null)
            {
                DebugConsole.LogWarning("[DirectConnection] Client not found: " + clientId);
                return false;
            }

            try
            {
                NetworkStream stream = client.GetStream();
                lock (stream)
                {
                    byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
                    stream.Write(lengthPrefix, 0, 4);
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }
                return true;
            }
            catch (Exception ex)
            {
                DebugConsole.LogError("[DirectConnection] Send to " + clientId + " error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 广播给所有客户端（主机用）
        /// </summary>
        public static void Broadcast(byte[] data)
        {
            foreach (var kvp in _connectedClients)
            {
                SendToClient(kvp.Key, data);
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取本机局域网 IP
        /// </summary>
        public static string GetLocalIPAddress()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ip))
                    {
                        string ipStr = ip.ToString();
                        if (ipStr.StartsWith("192.168.") ||
                            ipStr.StartsWith("10.") ||
                            ipStr.StartsWith("172."))
                        {
                            return ipStr;
                        }
                    }
                }
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogError("[DirectConnection] Failed to get local IP: " + ex.Message);
            }
            return "127.0.0.1";
        }

        /// <summary>
        /// 获取已连接的客户端数量
        /// </summary>
        public static int GetConnectedClientCount()
        {
            return _connectedClients.Count;
        }

        /// <summary>
        /// 获取连接状态信息
        /// </summary>
        public static string GetConnectionInfo()
        {
            if (IsServerRunning)
            {
                return "Server running - " + GetConnectedClientCount() + " clients";
            }
            else if (IsClientConnected)
            {
                return "Connected to host";
            }
            return "Not connected";
        }

        #endregion
    }
}
