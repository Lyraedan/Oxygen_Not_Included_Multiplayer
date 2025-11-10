using System;
using System.IO;
using Epic.OnlineServices;
using Epic.OnlineServices.Platform;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Auth;
using ONI_MP.DebugTools;
using System.Runtime.CompilerServices;
using static STRINGS.BUILDINGS.PREFABS.DOOR.CONTROL_STATE;
using System.Text;

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

        public System.Action OnLoginSuccessful;

        private EpicAccountId _epicAccountId;

        public EpicAccountId GetEpicAccountId() => _epicAccountId;

        private ContinuanceToken continuanceToken;
        private Token token;

        public bool LoggedIn = false;

        private static readonly string TokenPath = Path.Combine(
            Path.GetDirectoryName(typeof(Configuration).Assembly.Location),
            "token"
        );

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

            AttemptLoginWithPersistentAuth();
        }


        public void AttemptLoginWithPersistentAuth()
        {
            Epic.OnlineServices.Auth.LoginOptions loginOptions = new Epic.OnlineServices.Auth.LoginOptions()
            {
                Credentials = new Epic.OnlineServices.Auth.Credentials()
                {
                    Type = LoginCredentialType.PersistentAuth
                },
                ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList
            };

            _platformInterface.GetAuthInterface().Login(loginOptions, null, OnAuthLoginWithPersistentAuthComplete);
        }

        private void OnAuthLoginWithPersistentAuthComplete(Epic.OnlineServices.Auth.LoginCallbackInfo data)
        {
            if (data.ResultCode == Epic.OnlineServices.Result.Success)
            {
                DebugConsole.Log("[Epic Online Services] Logged in with persistent authorization successfully!");

                _epicAccountId = data.LocalUserId;
                continuanceToken = data.ContinuanceToken;

                _platformInterface.GetAuthInterface().CopyUserAuthToken(
                        new CopyUserAuthTokenOptions(),
                        data.LocalUserId,
                        out var token
                );
                this.token = token;

                OnLoginWasSuccessful();
            }
            else
            {
                DebugConsole.LogError("[Epic Online Services] Failed to log in with persistent authorization. Attempting log in through Account Portal. Error code: " + (int)data.ResultCode + " - " + data.ResultCode);
                LoginWithAccountPortal();
            }
        }

        public void LoginWithAccountPortal()
        {
            Epic.OnlineServices.Auth.LoginOptions loginOptions = new Epic.OnlineServices.Auth.LoginOptions()
            {
                Credentials = new Epic.OnlineServices.Auth.Credentials() {
                    Type = LoginCredentialType.AccountPortal
                },
                ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList
            };

            _platformInterface.GetAuthInterface().Login(loginOptions, null, OnAuthLoginWithAccountPortalAuthComplete);
        }

        private void OnAuthLoginWithAccountPortalAuthComplete(Epic.OnlineServices.Auth.LoginCallbackInfo data)
        {
            if (data.ResultCode == Epic.OnlineServices.Result.Success)
            {
                DebugConsole.Log("[Epic Online Services] Logged in through the account portal successfully!");
                _epicAccountId = data.LocalUserId;
                continuanceToken = data.ContinuanceToken;

                _platformInterface.GetAuthInterface().CopyUserAuthToken(
                        new CopyUserAuthTokenOptions(),
                        data.LocalUserId,
                        out var token
                );
                this.token = token;

                OnLoginWasSuccessful();
            }
            else
            {
                DebugConsole.LogError("[Epic Online Services]Failed to log in through the account portal. Error code:" + (int)data.ResultCode + " - " + data.ResultCode);
            }
        }

        private void OnLoginWasSuccessful()
        {
            //HandleConnect();
            CreateDeviceID(GetConnectInterface());
            //OnLoginSuccessful.Invoke();
            LoggedIn = true;
        }

        public void HandleConnect(ExternalCredentialType credentialType = ExternalCredentialType.Epic)
        {
            var connect = _platformInterface.GetConnectInterface();
            if (continuanceToken != null)
            {
                CreateUser(connect, continuanceToken);
            }
            else
            {
                DebugConsole.Log($"Access token: " + token.AccessToken);
                var connectLoginOptions = new Epic.OnlineServices.Connect.LoginOptions
                {
                    Credentials = new Epic.OnlineServices.Connect.Credentials
                    {
                        Type = credentialType, // .Epic (Would crash)
                        Token = null
                    }
                };
                connect.Login(connectLoginOptions, null, OnConnectLoginComplete);
            }

        }

        private void OnConnectLoginComplete(Epic.OnlineServices.Connect.LoginCallbackInfo data)
        {
            DebugConsole.Log("Connect login successful!!!!!!!!!!");
            if(data.LocalUserId == null)
            {
                DebugConsole.Log($"Local user id is null!\nContinuance Token: {(data.ContinuanceToken == null ? "null" : "valid")}\nCached Continuance Token: {(continuanceToken == null ? "null" : "valid")}");
            }
            _localUserId = data.LocalUserId;

            //_initialized = true;
            OnLoginSuccessful.Invoke();
        }

        public void Logout()
        {
            LogoutOptions logoutOptions = new LogoutOptions()
            {
                LocalUserId = _epicAccountId
            };
            _platformInterface.GetAuthInterface().Logout(logoutOptions, null, OnAuthLogOutCompleted);
        }

        private void OnAuthLogOutCompleted(LogoutCallbackInfo data)
        {
            DebugConsole.Log("[Epic Online Services] Successfully logged out!");
            ClearPersistentAuth();
            LoggedIn = false;
        }

        public void ClearPersistentAuth()
        {
            DeletePersistentAuthOptions deletePersistentAuthOptions = new DeletePersistentAuthOptions()
            {

            };
            _platformInterface.GetAuthInterface().DeletePersistentAuth(deletePersistentAuthOptions, null, OnClearedPersistentAuth);
        }

        private void OnClearedPersistentAuth(DeletePersistentAuthCallbackInfo data)
        {
            DebugConsole.Log("[Epic Online Services] Cleared persistent auth");
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

        public void CreateDeviceID(ConnectInterface connect)
        {
            var options = new CreateDeviceIdOptions();

            Random random = new Random();

            var userLoginInfo = new UserLoginInfo
            {
                DisplayName = "Anonymous Player" // Set to display name from auth
            };

            var createDeviceIdOptions = new CreateDeviceIdOptions
            {
                DeviceModel = "MyCustomGame-v1.0",
            };

            connect.CreateDeviceId(createDeviceIdOptions, null, OnCreateDeviceIdComplete);
        }

        private void OnCreateDeviceIdComplete(CreateDeviceIdCallbackInfo data)
        {
            if (data.ResultCode == Epic.OnlineServices.Result.Success)
            {
                DebugConsole.Log("Device ID successfully created or retrieved.");

                HandleConnect(ExternalCredentialType.DeviceidAccessToken);
            }
            else
            {
                // Handle failure to create/retrieve device ID
                DebugConsole.Log($"Failed to create Device ID: {data.ResultCode}");
            }
        }
    }
}
