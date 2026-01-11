using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Networking
{
	public interface IBulkablePacket
	{
		int MaxPackSize { get; }
		uint IntervalMs { get; }
	}
}
