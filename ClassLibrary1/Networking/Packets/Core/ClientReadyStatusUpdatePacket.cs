using ONI_MP.Menus;
using ONI_MP.Misc.World;
using ONI_MP.Networking.Packets.Architecture;
using System.IO;
using Shared.Profiling;

namespace ONI_MP.Networking.Packets.Core
{
	public class ClientReadyStatusUpdatePacket : IPacket
	{
		public string Message;

		public ClientReadyStatusUpdatePacket() { }

		public ClientReadyStatusUpdatePacket(string message)
		{
			Profiler.Scope();

			Message = message;
		}

		public void Serialize(BinaryWriter writer)
		{
			Profiler.Scope();

			writer.Write(Message);
		}

		public void Deserialize(BinaryReader reader)
		{
			Profiler.Scope();

			Message = reader.ReadString();
		}

		public void OnDispatched()
		{
			Profiler.Scope();

			// Host updates theirs on each ready status packet so we dont do anything here
			if (MultiplayerSession.IsHost)
				return;

			// We are actively downloading the save file, ignore
			if (SaveChunkAssembler.isDownloading)
				return;

			MultiplayerOverlay.Show(Message);
		}
	}
}
