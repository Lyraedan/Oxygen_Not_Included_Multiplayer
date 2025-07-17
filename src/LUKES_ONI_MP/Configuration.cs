using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace ONI_MP
{
    class Configuration
    {
        private static readonly string ConfigPath = Path.Combine(
            Path.GetDirectoryName(typeof(Configuration).Assembly.Location),
            "multiplayer_settings.json"
        );
        private static Configuration _instance;

        public HostSettings Host { get; set; } = new HostSettings();
        public ClientSettings Client { get; set; } = new ClientSettings();

        public static Configuration Instance
        {
            get
            {
                if (_instance == null)
                    _instance = LoadOrCreate();
                return _instance;
            }
        }

        public static T GetHostProperty<T>(string propertyName)
        {
            if (Instance?.Host == null)
            {
                Debug.LogWarning($"[Configuration] Host settings are null, creating default instance");
                if (Instance != null)
                    Instance.Host = new HostSettings();
                else
                    _instance = new Configuration();
            }
            return Instance.GetProperty<T>(Instance.Host, propertyName);
        }

        public static T GetClientProperty<T>(string propertyName)
        {
            if (Instance?.Client == null)
            {
                Debug.LogWarning($"[Configuration] Client settings are null, creating default instance");
                if (Instance != null)
                    Instance.Client = new ClientSettings();
                else
                    _instance = new Configuration();
            }
            return Instance.GetProperty<T>(Instance.Client, propertyName);
        }

        public static T GetCloudStorageProperty<T>(string propertyName)
        {
            if (Instance?.Host?.CloudStorage == null)
            {
                Debug.LogWarning($"[Configuration] CloudStorage settings are null, creating default instance");
                if (Instance?.Host != null)
                    Instance.Host.CloudStorage = new CloudStorageSettings();
                else
                {
                    if (Instance != null)
                        Instance.Host = new HostSettings();
                    else
                        _instance = new Configuration();
                }
            }
            return Instance.GetProperty<T>(Instance.Host.CloudStorage, propertyName);
        }

        public static T GetGoogleDriveProperty<T>(string propertyName)
        {
            if (Instance?.Host?.CloudStorage?.GoogleDrive == null)
            {
                Debug.LogWarning($"[Configuration] GoogleDrive settings are null, creating default instance");
                if (Instance?.Host?.CloudStorage != null)
                    Instance.Host.CloudStorage.GoogleDrive = new GoogleDriveSettings();
                else
                {
                    if (Instance?.Host != null)
                        Instance.Host.CloudStorage = new CloudStorageSettings();
                    else
                    {
                        if (Instance != null)
                            Instance.Host = new HostSettings();
                        else
                            _instance = new Configuration();
                    }
                }
            }
            return Instance.GetProperty<T>(Instance.Host.CloudStorage.GoogleDrive, propertyName);
        }

        public static T GetStorageServerProperty<T>(string propertyName)
        {
            if (Instance?.Host?.CloudStorage?.StorageServer == null)
            {
                Debug.LogWarning($"[Configuration] StorageServer settings are null, creating default instance");
                if (Instance?.Host?.CloudStorage != null)
                    Instance.Host.CloudStorage.StorageServer = new StorageServerSettings();
                else
                {
                    if (Instance?.Host != null)
                        Instance.Host.CloudStorage = new CloudStorageSettings();
                    else
                    {
                        if (Instance != null)
                            Instance.Host = new HostSettings();
                        else
                            _instance = new Configuration();
                    }
                }
            }
            return Instance.GetProperty<T>(Instance.Host.CloudStorage.StorageServer, propertyName);
        }

        public static T GetSteamP2PProperty<T>(string propertyName)
        {
            if (Instance?.Host?.CloudStorage?.SteamP2P == null)
            {
                Debug.LogWarning($"[Configuration] SteamP2P settings are null, creating default instance");
                if (Instance?.Host?.CloudStorage != null)
                    Instance.Host.CloudStorage.SteamP2P = new SteamP2PSettings();
                else
                {
                    if (Instance?.Host != null)
                        Instance.Host.CloudStorage = new CloudStorageSettings();
                    else
                    {
                        if (Instance != null)
                            Instance.Host = new HostSettings();
                        else
                            _instance = new Configuration();
                    }
                }
            }
            return Instance.GetProperty<T>(Instance.Host.CloudStorage.SteamP2P, propertyName);
        }

        public static void SetCloudStorageProvider(string provider)
        {
            try
            {
                if (Instance?.Host?.CloudStorage != null)
                {
                    Instance.Host.CloudStorage.Provider = provider;
                    Instance.Save();
                    Debug.Log($"[Configuration] Changed provider to {provider}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Configuration] Failed to set provider: {ex.Message}");
            }
        }

        private T GetProperty<T>(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), $"Cannot get property '{propertyName}' from null object");
                
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (prop == null)
                throw new ArgumentException($"Property '{propertyName}' not found on {obj.GetType().Name}");

            if (!typeof(T).IsAssignableFrom(prop.PropertyType))
                throw new InvalidCastException($"Property '{propertyName}' is of type {prop.PropertyType}, not {typeof(T)}");

            return (T)prop.GetValue(obj);
        }

        public static Configuration LoadOrCreate()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new Configuration();
                string json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
                return defaultConfig;
            }

            try
            {
                string existingJson = File.ReadAllText(ConfigPath);
                var config = JsonConvert.DeserializeObject<Configuration>(existingJson);
                
                // Ensure properties are not null after deserialization
                if (config == null)
                {
                    Debug.LogWarning("[Configuration] Deserialized config is null, creating new instance");
                    config = new Configuration();
                }
                
                if (config.Host == null)
                {
                    Debug.LogWarning("[Configuration] Host settings are null after deserialization, creating default");
                    config.Host = new HostSettings();
                }
                
                if (config.Client == null)
                {
                    Debug.LogWarning("[Configuration] Client settings are null after deserialization, creating default");
                    config.Client = new ClientSettings();
                }
                
                if (config.Host.CloudStorage == null)
                {
                    Debug.LogWarning("[Configuration] CloudStorage settings are null after deserialization, creating default");
                    config.Host.CloudStorage = new CloudStorageSettings();
                }
                
                if (config.Host.CloudStorage.GoogleDrive == null)
                {
                    Debug.LogWarning("[Configuration] GoogleDrive settings are null after deserialization, creating default");
                    config.Host.CloudStorage.GoogleDrive = new GoogleDriveSettings();
                }
                
                if (config.Host.CloudStorage.StorageServer == null)
                {
                    Debug.LogWarning("[Configuration] StorageServer settings are null after deserialization, creating default");
                    config.Host.CloudStorage.StorageServer = new StorageServerSettings();
                }
                
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Configuration] Failed to load configuration: {ex.Message}. Creating default configuration.");
                var defaultConfig = new Configuration();
                string json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
                return defaultConfig;
            }
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }
    }

    class HostSettings
    {
        public int MaxLobbySize { get; set; } = 4;
        public int MaxMessagesPerPoll { get; set; } = 128;
        public int SaveFileTransferChunkKB { get; set; } = 256;

        public CloudStorageSettings CloudStorage { get; set; } = new CloudStorageSettings();
        
        // Keep GoogleDrive for backward compatibility
        [JsonIgnore]
        public GoogleDriveSettings GoogleDrive 
        { 
            get => CloudStorage?.GoogleDrive ?? new GoogleDriveSettings();
            set 
            {
                if (CloudStorage == null)
                    CloudStorage = new CloudStorageSettings();
                CloudStorage.GoogleDrive = value;
            }
        }
    }

    class ClientSettings
    {
        public bool UseCustomMainMenu { get; set; } = true;
        public int MaxMessagesPerPoll { get; set; } = 16;
        public bool UseRandomPlayerColor { get; set; } = true;
        public ColorRGB PlayerColor { get; set; } = new ColorRGB(255, 255, 255);
    }

    class CloudStorageSettings
    {
        public string Provider { get; set; } = "SteamP2P"; // "GoogleDrive", "StorageServer", or "SteamP2P"
        public GoogleDriveSettings GoogleDrive { get; set; } = new GoogleDriveSettings();
        public StorageServerSettings StorageServer { get; set; } = new StorageServerSettings();
        public SteamP2PSettings SteamP2P { get; set; } = new SteamP2PSettings();
    }

    class GoogleDriveSettings
    {
        public string ApplicationName { get; set; } = "ONI Multiplayer Mod";
    }

    class StorageServerSettings
    {
        public string HttpServerUrl { get; set; } = "http://localhost:3000"; // Server URL
        public string SessionId { get; set; } = ""; // Auto-generated if empty
        public string AuthToken { get; set; } = ""; // Optional authentication token
    }

    class SteamP2PSettings
    {
        public int ChunkSizeKB { get; set; } = 256; // File transfer chunk size in KB
        public bool EnableCompression { get; set; } = true; // Enable file compression
        public int TransferTimeoutSeconds { get; set; } = 60; // Transfer timeout
    }


    class ColorRGB
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public ColorRGB() { }

        public ColorRGB(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public Color ToColor() => new Color(R / 255f, G / 255f, B / 255f);
        public static ColorRGB FromColor(Color color) =>
            new ColorRGB((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255));
    }
}
