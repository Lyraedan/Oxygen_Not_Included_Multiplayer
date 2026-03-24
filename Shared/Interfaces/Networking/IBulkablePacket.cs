using ONI_MP.Networking.Packets.Architecture;

namespace Shared.Interfaces.Networking
{
	public interface IBulkablePacket : IPacket
	{
		int MaxPackSize { get; }
		uint IntervalMs { get; }
	}
}
