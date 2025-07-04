﻿using System;
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
        public GoogleDriveSettings GoogleDrive { get; set; } = new GoogleDriveSettings();

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
            return Instance.GetProperty<T>(Instance.Host, propertyName);
        }

        public static T GetClientProperty<T>(string propertyName)
        {
            return Instance.GetProperty<T>(Instance.Client, propertyName);
        }

        public static T GetGoogleDriveProperty<T>(string propertyName)
        {
            return Instance.GetProperty<T>(Instance.GoogleDrive, propertyName);
        }

        private T GetProperty<T>(object obj, string propertyName)
        {
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

            string existingJson = File.ReadAllText(ConfigPath);
            return JsonConvert.DeserializeObject<Configuration>(existingJson);
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
        public string CredentialsPath { get; set; } = "";
        public string TokenPath { get; set; } = "";
        public string DriveFolderId { get; set; } = "";
        public string ApplicationName { get; set; } = "ONI Multiplayer Mod"; // default
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
