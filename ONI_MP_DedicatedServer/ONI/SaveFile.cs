using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Profiling;

namespace ONI_MP_DedicatedServer.ONI
{
    public class SaveFile
    {
        public byte[] Data { get; set; }
        public string Name { get; set; }

        public SaveFile(string name, byte[] data)
        {
            Profiler.Scope();

            Name = name;
            Data = data;
        }

        public static SaveFile FromFile(string filePath)
        {
            Profiler.Scope();

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Save file not found: {filePath}");

            string name = Path.GetFileName(filePath);
            byte[] data = File.ReadAllBytes(filePath);

            Console.WriteLine($"Loaded save file: {name} @ {Utils.FormatBytes(data.Length)}");
            return new SaveFile(name, data);
        }
    }
}
