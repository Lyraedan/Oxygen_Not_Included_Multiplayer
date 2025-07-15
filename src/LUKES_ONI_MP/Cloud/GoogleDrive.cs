using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using ONI_MP.DebugTools;
using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace ONI_MP.Cloud
{
    public class GoogleDrive
    {
        private static readonly string CredentialsPath = Path.Combine(
            Path.GetDirectoryName(typeof(Configuration).Assembly.Location),
            "credentials.json"
        );

        private static readonly string TokenPath = Path.Combine(
            Path.GetDirectoryName(typeof(Configuration).Assembly.Location),
            "token"
        );

        private static GoogleDrive _instance;
        private DriveService _service;
        private string _applicationName;
        private bool _initialized = false;

        public bool IsInitialized => _initialized;

        private GoogleDrive() { }

        public static GoogleDrive Instance => _instance ?? (_instance = new GoogleDrive());

        public DriveService Service => _service;

        public GoogleDriveUploader Uploader { get; private set; }
        public GoogleDriveDownloader Downloader { get; private set; }
        public UnityEvent OnInitialized { get; } = new UnityEvent();

        /// <summary>
        /// Initializes the Google Drive service using user-provided credentials and token,
        /// and an ApplicationName pulled from the GoogleDriveSettings.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                DebugConsole.Log("GoogleDrive already initialized.");
                return;
            }

            if (!File.Exists(CredentialsPath))
            {
                DebugConsole.LogError($"GoogleDrive: credentials.json not found at {CredentialsPath}", false);
                //throw new FileNotFoundException("credentials.json missing", credentialsPath);
                return;
            }

            _applicationName = Configuration.GetGoogleDriveProperty<string>("ApplicationName");
            if (string.IsNullOrWhiteSpace(_applicationName))
            {
                _applicationName = "ONI Multiplayer Mod";
            }

            UserCredential credential;
            using (var stream = new FileStream(CredentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { DriveService.Scope.DriveFile },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(TokenPath, true)
                ).Result;
            }

            _service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _applicationName
            });

            Uploader = new GoogleDriveUploader(_service);
            Downloader = new GoogleDriveDownloader(_service);

            DebugConsole.Log($"GoogleDrive: Initialized successfully with application name '{_applicationName}'.");
            _initialized = _service != null;
            OnInitialized.Invoke();
        }
    }
}
