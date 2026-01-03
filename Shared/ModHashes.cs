using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
	public class ModHashes
	{
		private readonly int value;
		private readonly string name;
		private readonly GameHashes hash;

		public ModHashes(string name)
		{
			this.name = name;
			value = Hash.SDBMLower(name);
			hash = (GameHashes)value;
		}

		public static implicit operator GameHashes(ModHashes modHashes)
		{
			return modHashes.hash;
		}

		public static implicit operator int(ModHashes modHashes)
		{
			return modHashes.value;
		}

		public static implicit operator string(ModHashes modHashes)
		{
			return modHashes.name;
		}

		public override string ToString()
		{
			return name;
		}
	}
}
