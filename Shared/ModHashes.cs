using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Profiling;

namespace Shared
{
	public class ModHashes
	{
		private readonly int value;
		private readonly string name;
		private readonly GameHashes hash;

		public ModHashes(string name)
		{
			using var _ = Profiler.Scope();

			this.name = name;
			value = Hash.SDBMLower(name);
			hash = (GameHashes)value;
		}

		public static implicit operator GameHashes(ModHashes modHashes)
		{
			using var _ = Profiler.Scope();

			return modHashes.hash;
		}

		public static implicit operator int(ModHashes modHashes)
		{
			using var _ = Profiler.Scope();

			return modHashes.value;
		}

		public static implicit operator string(ModHashes modHashes)
		{
			using var _ = Profiler.Scope();

			return modHashes.name;
		}

		public override string ToString()
		{
			using var _ = Profiler.Scope();

			return name;
		}
	}
}
