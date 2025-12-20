using System;
using System.IO;
using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Packets.Architecture
{

	public static class PacketHandler
	{
		public static bool readyToProcess = true;

		public static void HandleIncoming(byte[] data)
		{
			if (!readyToProcess)
			{
				return;
			}

			using (var ms = new MemoryStream(data))
			{
				using (var reader = new BinaryReader(ms))
				{
					int type = (int)reader.ReadInt32();
                    if (!PacketRegistry.HasRegisteredPacket(type))
                    {
                        DebugConsole.LogError($"Invalid PacketType received: {type}", false);
                        return;
                    }

                    var packet = PacketRegistry.Create(type);
					packet.Deserialize(reader);
					Dispatch(packet);
				}
			}
		}

		private static void Dispatch(IPacket packet)
		{
			packet.OnDispatched();
		}
	}

}
