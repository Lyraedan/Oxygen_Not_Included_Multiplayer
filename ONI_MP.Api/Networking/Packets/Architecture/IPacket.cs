using System.IO;

namespace ONI_MP.Api.Networking.Packets.Architecture
{
	public interface IPacket
	{
		void Serialize(BinaryWriter writer);
		void Deserialize(BinaryReader reader);

		void OnDispatched();

	}

}
