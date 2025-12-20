using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.Menus;
using System.IO;

namespace ONI_MP.Networking.Packets.Core
{
	public class ClientReadyStatusUpdatePacket : IPacket
	{
		public string Message;

		public ClientReadyStatusUpdatePacket() { }

		public ClientReadyStatusUpdatePacket(string message)
		{
			Message = message;
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Message);
		}

		public void Deserialize(BinaryReader reader)
		{
			Message = reader.ReadString();
		}

		public void OnDispatched()
		{
			// Host updates theirs on each ready status packet so we dont do anything here
			if (MultiplayerSession.IsHost)
				return;

			MultiplayerOverlay.Show(Message);
		}
	}
}
