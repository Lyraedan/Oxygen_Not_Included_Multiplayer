using System;
using Epic.OnlineServices;
using Epic.OnlineServices.Platform;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.P2P;
using ONI_MP.DebugTools;
using Epic.OnlineServices.Auth;

namespace ONI_MP.Networking.Relay.Platforms.EOS
{
    public class EOSManager
    {
        public static EOSManager Instance { get; private set; }

        private PlatformInterface _platformInterface;
        private ProductUserId _localUserId;
        private bool _initialized = false;

        public bool IsInitialized => _initialized;
        public ProductUserId GetLocalUserId() => _localUserId;
        public P2PInterface GetP2PInterface() => _platformInterface?.GetP2PInterface();
        public ConnectInterface GetConnectInterface() => _platformInterface?.GetConnectInterface();

        public EOSManager()
        {
            if (Instance != null)
                throw new InvalidOperationException("EOSManager already created");

            Instance = this;
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            var options = new InitializeOptions
            {
                ProductName = "ONI_MP",
                ProductVersion = "1.0"
            };

            var initResult = Epic.OnlineServices.Platform.PlatformInterface.Initialize(options);
            if (initResult != Epic.OnlineServices.Result.Success)
            {
                DebugConsole.LogError($"[EOSManager] EOS Initialize failed: {initResult}");
                return;
            }

            var platformOptions = new Options
            {
                ProductId = "<your-product-id>",
                SandboxId = "<your-sandbox-id>",
                DeploymentId = "<your-deployment-id>",
                ClientCredentials = new ClientCredentials
                {
                    ClientId = "<your-client-id>",
                    ClientSecret = "<your-client-secret>"
                },
                IsServer = false,
                EncryptionKey = "0123456789abcdef0123456789abcdef", // 32 bytes
                CacheDirectory = "EOSCache"
            };

            _platformInterface = Epic.OnlineServices.Platform.PlatformInterface.Create(platformOptions);
            if (_platformInterface == null)
            {
                DebugConsole.LogError("[EOSManager] Failed to create EOS platform interface.");
                return;
            }

            LoginWithConnect();
        }

        private void LoginWithConnect()
        {
            var connect = _platformInterface.GetConnectInterface();

            var loginOptions = new Epic.OnlineServices.Connect.LoginOptions
            {
                Credentials = new Epic.OnlineServices.Connect.Credentials
                {
                    Type = ExternalCredentialType.DeviceidAccessToken,
                    Token = null
                }
            };

            connect.Login(loginOptions, null, result =>
            {
                if (result.ResultCode == Epic.OnlineServices.Result.Success)
                {
                    _localUserId = result.LocalUserId;
                    _initialized = true;
                    DebugConsole.Log($"[EOSManager] Login successful. UserId = {_localUserId}");
                }
                else
                {
                    DebugConsole.LogError($"[EOSManager] Login failed: {result.ResultCode}");
                }
            });
        }
    }
}
