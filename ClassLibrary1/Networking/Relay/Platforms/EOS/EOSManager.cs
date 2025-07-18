using System;
using System.IO;
using Epic.OnlineServices;
using Epic.OnlineServices.Platform;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Auth;
using ONI_MP.DebugTools;

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
        public PlatformInterface GetPlatformInterface() => _platformInterface;

        public System.Action OnCreatedDeviceId;
        public System.Action OnLoginSuccessful;

        private EpicAccountId _epicAccountId;

        public EpicAccountId GetEpicAccountId() => _epicAccountId;

        private static readonly string TokenPath = Path.Combine(
            Path.GetDirectoryName(typeof(Configuration).Assembly.Location),
            "token"
        );

        private static readonly string RefreshTokenFile = Path.Combine(TokenPath, "eos.refreshtoken");

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
                ProductName = "Oxygen Not Included Together",
                ProductVersion = "1.0"
            };

            var initResult = Epic.OnlineServices.Platform.PlatformInterface.Initialize(options);
            if (initResult != Epic.OnlineServices.Result.Success)
            {
                DebugConsole.LogError($"[EOSManager] EOS Initialize failed: {initResult}");
                return;
            }

            EOSConfig config = EOSConfig.LoadFromEmbeddedResource("ONI_MP.Assets.eos_config.json");

            var platformOptions = new Epic.OnlineServices.Platform.Options
            {
                ProductId = config.Options.ProductId,
                SandboxId = config.Options.SandboxId,
                DeploymentId = config.Options.DeploymentId,
                ClientCredentials = new Epic.OnlineServices.Platform.ClientCredentials
                {
                    ClientId = config.Options.ClientCredentials.ClientId,
                    ClientSecret = config.Options.ClientCredentials.ClientSecret
                }
            };

            _platformInterface = Epic.OnlineServices.Platform.PlatformInterface.Create(platformOptions);
            if (_platformInterface == null)
            {
                DebugConsole.LogError("[EOSManager] Failed to create EOS platform interface.");
                return;
            }

            _initialized = true;
            Login();
        }

        private void SaveRefreshToken(string token)
        {
            try
            {
                if (!Directory.Exists(TokenPath))
                    Directory.CreateDirectory(TokenPath);

                File.WriteAllText(RefreshTokenFile, token ?? string.Empty);
                DebugConsole.Log("[EOSManager] Saved refresh token to disk.");
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[EOSManager] Failed to save refresh token: {ex}");
            }
        }

        private string LoadRefreshToken()
        {
            try
            {
                if (File.Exists(RefreshTokenFile))
                    return File.ReadAllText(RefreshTokenFile);
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[EOSManager] Failed to load refresh token: {ex}");
            }

            return null;
        }

        public void Login()
        {
            var auth = _platformInterface.GetAuthInterface();
            string savedRefreshToken = LoadRefreshToken();

            var loginOptions = new Epic.OnlineServices.Auth.LoginOptions
            {
                Credentials = new Epic.OnlineServices.Auth.Credentials
                {
                    Type = string.IsNullOrEmpty(savedRefreshToken)
                        ? LoginCredentialType.AccountPortal
                        : LoginCredentialType.RefreshToken,
                    Token = savedRefreshToken,
                    Id = null
                },
                ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList
            };

            auth.Login(loginOptions, null, loginResult =>
            {
                if (loginResult.ResultCode == Epic.OnlineServices.Result.Success)
                {
                    DebugConsole.Log($"[EOSManager] Epic login successful. EpicAccountId = {loginResult.LocalUserId}");

                    auth.CopyUserAuthToken(
                        new CopyUserAuthTokenOptions(),
                        loginResult.LocalUserId,
                        out var token
                    );

                    if (!string.IsNullOrEmpty(token.RefreshToken))
                    {
                        SaveRefreshToken(token.RefreshToken);
                        DebugConsole.Log("[EOSManager] Refresh token saved.");
                    }

                    _epicAccountId = loginResult.LocalUserId;

                    var connect = _platformInterface.GetConnectInterface();

                    if (loginResult.ContinuanceToken != null)
                    {
                        var linkOptions = new CreateUserOptions
                        {
                            ContinuanceToken = loginResult.ContinuanceToken
                        };

                        connect.CreateUser(linkOptions, null, linkResult =>
                        {
                            if (linkResult.ResultCode == Epic.OnlineServices.Result.Success)
                            {
                                _localUserId = linkResult.LocalUserId;
                                _initialized = true;
                                OnLoginSuccessful?.Invoke();
                                DebugConsole.Log($"[EOSManager] Created new EOS user. ProductUserId = {_localUserId}");
                            }
                            else
                            {
                                DebugConsole.LogError($"[EOSManager] CreateUser failed: {linkResult.ResultCode}");
                            }
                        });
                    }
                }
                else
                {
                    DebugConsole.LogError($"[EOSManager] Epic login failed: {loginResult.ResultCode}");
                    SaveRefreshToken(null); // Clear bad token
                }
            });
        }
    }
}
