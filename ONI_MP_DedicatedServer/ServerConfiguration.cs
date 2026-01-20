using System;
using System.IO;
using Newtonsoft.Json;

namespace ONI_MP_DedicatedServer
{
    public class ServerConfiguration
    {
        private static ServerConfiguration _instance;
        public static ServerConfiguration Instance
        {
            get
            {
                if (_instance == null)
                    _instance = LoadOrCreate();
                return _instance;
            }
        }

        public DedicatedConfig Config { get; private set; } = new DedicatedConfig();

        private static readonly string ConfigDirectory = Path.Combine(AppContext.BaseDirectory, "dedicated_server_config");

        private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "multiplayer_settings.json");

        public static ServerConfiguration LoadOrCreate()
        {
            if (!Directory.Exists(ConfigDirectory))
                Directory.CreateDirectory(ConfigDirectory);

            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new ServerConfiguration();
                defaultConfig.Save();
                return defaultConfig;
            }

            string existingJson = File.ReadAllText(ConfigPath);
            var loaded = JsonConvert.DeserializeObject<ServerConfiguration>(existingJson);

            if (loaded == null)
                return new ServerConfiguration();

            return loaded;
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }
    }

    public class DedicatedConfig
    {
        public int Transport = 0;
        public string Ip = "127.0.0.1";
        public int Port = 7777;

        public int MaxLobbySize { get; set; } = 4;
        public int MaxMessagesPerPoll { get; set; } = 128;
    }
}
