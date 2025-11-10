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
        public System.Action<ContinuanceToken> OnAuthLoginSuccessful;
        public System.Action OnLoginSuccessful;

        private EpicAccountId _epicAccountId;

        public EpicAccountId GetEpicAccountId() => _epicAccountId;

        private Token token;

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

            // WHY ON EARTH DO YOU CRASH
            //OnAuthLoginSuccessful += HandleAuthLoginSuccess;

            OnAuthLoginSuccessful += (continuanceToken) =>
            {
                //var connect = _platformInterface.GetConnectInterface();
                //ConnectLogin(connect);
            };
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
            //Login_Persistent();
            Login_AccountPortal();
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

        public void Login_Persistent()
        {
            Login(LoginCredentialType.PersistentAuth);
        }

        public void Login_AccountPortal()
        {
            Login(LoginCredentialType.AccountPortal);
        }

        public void Login(LoginCredentialType credentialsType)
        {
            var auth = _platformInterface.GetAuthInterface();

            var loginOptions = new Epic.OnlineServices.Auth.LoginOptions
            {
                Credentials = new Epic.OnlineServices.Auth.Credentials
                {
                    Type = credentialsType
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

                    _epicAccountId = loginResult.LocalUserId;
                    this.token = token;
                    OnAuthLoginSuccessful.Invoke(loginResult.ContinuanceToken);
                }
                else
                {
                    DebugConsole.LogError($"[EOSManager] Epic login failed: {loginResult.ResultCode}");
                    if(credentialsType == LoginCredentialType.PersistentAuth)
                    {
                        Login_AccountPortal();
                    }
                }
            });
        }

        public void HandleAuthLoginSuccess(ContinuanceToken continueanceToken)
        {
            var connect = _platformInterface.GetConnectInterface();

            if (continueanceToken != null)
            {
                CreateUser(connect, continueanceToken);
            }
            else
            {
                ConnectLogin(connect);
            }
        }

        private void CreateUser(ConnectInterface connect, ContinuanceToken continuanceToken)
        {
            var linkOptions = new CreateUserOptions
            {
                ContinuanceToken = continuanceToken
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

        public void ManualConnectLogin()
        {
            var connect = _platformInterface.GetConnectInterface();
            ConnectLogin(connect);
        }

        private void ConnectLogin(ConnectInterface connect)
        {
            if(string.IsNullOrEmpty(this.token.AccessToken))
            {
                DebugConsole.LogError("Failed to connect login! Access token is invalid!");
                return;
            }

            var connectLoginOptions = new Epic.OnlineServices.Connect.LoginOptions
            {
                Credentials = new Epic.OnlineServices.Connect.Credentials
                {
                    Type = Epic.OnlineServices.Connect.ExternalCredentialType.Epic,
                    Token = token.AccessToken
                }
            };

            try
            {
                connect.Login(connectLoginOptions, null, connectLoginResult =>
                {
                    DebugConsole.Log($"[EOSManager] Connect.Login callback received. Result = {connectLoginResult.ResultCode}");
                    if (connectLoginResult.ResultCode == Epic.OnlineServices.Result.Success ||
                    connectLoginResult.ResultCode == Epic.OnlineServices.Result.AlreadyConfigured)
                    {
                        _localUserId = connectLoginResult.LocalUserId;
                        _initialized = true;
                        OnLoginSuccessful?.Invoke();
                        DebugConsole.Log($"[EOSManager] Connected existing EOS user. ProductUserId = {_localUserId}");
                    }
                    else if (connectLoginResult.ResultCode == Epic.OnlineServices.Result.InvalidUser)
                    {
                        DebugConsole.LogWarning("[EOSManager] Connect.Login returned InvalidUser — need to create a new one.");
                        CreateUser(connect, connectLoginResult.ContinuanceToken);
                    }
                    else
                    {
                        DebugConsole.LogError($"[EOSManager] Connect.Login failed: {connectLoginResult.ResultCode}");
                    }
                });
            } catch(Exception ex)
            {
                DebugConsole.LogError($"[EOSManager] Failed to connect login: {ex}");
            }
        }

        public void Login_OLD()
        {
            var auth = _platformInterface.GetAuthInterface();
            string savedRefreshToken = LoadRefreshToken();

            var loginOptions = new Epic.OnlineServices.Auth.LoginOptions
            {
                Credentials = new Epic.OnlineServices.Auth.Credentials
                {
                    Type = string.IsNullOrEmpty(savedRefreshToken)
                        ? LoginCredentialType.AccountPortal
                        : LoginCredentialType.PersistentAuth,
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
