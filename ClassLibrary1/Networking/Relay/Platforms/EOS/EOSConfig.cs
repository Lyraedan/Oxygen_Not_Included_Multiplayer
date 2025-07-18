using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ONI_MP.Networking.Relay.Platforms.EOS
{
    public class EOSConfig
    {
        public Options Options { get; set; }

        public static EOSConfig LoadFromEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<EOSConfig>(json);
                }
            }
        }
    }

    public class Options
    {
        public string ProductId { get; set; }
        public string SandboxId { get; set; }
        public string DeploymentId { get; set; }
        public ClientCredentials ClientCredentials { get; set; }
    }

    public class ClientCredentials
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

}
