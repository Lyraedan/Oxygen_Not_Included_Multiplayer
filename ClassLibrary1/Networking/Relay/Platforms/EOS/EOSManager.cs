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
                ProductId = "d6ef1606c8284101a70dc560c2e990a0",
                SandboxId = "5faf3ea257d349818d87aaf5517810e0",
                DeploymentId = "b30a90d34ae34c92b4d9259c6540b037",
                ClientCredentials = new ClientCredentials
                {
                    ClientId = "xyza7891UPTBdO5vIQGLOExreRxe5fGB",
                    ClientSecret = "z/DAJ5j/22iEZTllVl3yCA1wV0x8BNX9Ijhk/My5cBg"
                }
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
