using System.IO;
using Shared.Profiling;

namespace ONI_MP.Misc.World
{
	public class WorldSave
	{
		public byte[] Data { get; set; }
		public string Name { get; set; }

		public WorldSave(string name, byte[] data)
		{
			Profiler.Scope();

			Name = name;
			Data = data;
		}

		public static WorldSave FromFile(string filePath)
		{
			Profiler.Scope();

			if (!File.Exists(filePath))
				throw new FileNotFoundException($"Save file not found: {filePath}");

			string name = Path.GetFileName(filePath);
			byte[] data = File.ReadAllBytes(filePath);
			return new WorldSave(name, data);
		}
	}
}
