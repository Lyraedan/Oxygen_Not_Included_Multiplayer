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

        public static T GetGoogleDriveProperty<T>(string propertyName)
        {
            if (Instance?.Host?.GoogleDrive == null)
            {
                Debug.LogWarning($"[Configuration] GoogleDrive settings are null, creating default instance");
                if (Instance?.Host != null)
                    Instance.Host.GoogleDrive = new GoogleDriveSettings();
                else
                {
                    if (Instance != null)
                        Instance.Host = new HostSettings();
                    else
                        _instance = new Configuration();
                }
            }
            return Instance.GetProperty<T>(Instance.Host.GoogleDrive, propertyName);
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
                
                if (config.Host.GoogleDrive == null)
                {
                    Debug.LogWarning("[Configuration] GoogleDrive settings are null after deserialization, creating default");
                    config.Host.GoogleDrive = new GoogleDriveSettings();
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

        public GoogleDriveSettings GoogleDrive { get; set; } = new GoogleDriveSettings();
    }

    class ClientSettings
    {
        public bool UseCustomMainMenu { get; set; } = true;
        public int MaxMessagesPerPoll { get; set; } = 16;
        public bool UseRandomPlayerColor { get; set; } = true;
        public ColorRGB PlayerColor { get; set; } = new ColorRGB(255, 255, 255);
    }

    class GoogleDriveSettings
    {
        public string ApplicationName { get; set; } = "ONI Multiplayer Mod";
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
