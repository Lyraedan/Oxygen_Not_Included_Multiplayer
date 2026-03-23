using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using Shared.Profiling;

namespace ONI_MP.Networking.Packets.Architecture
{

	public static class PacketHandler
	{
		public static bool readyToProcess = true;

		public static void HandleIncoming(byte[] data)
		{
			Profiler.Scope();

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

                    using var scope = Profiler.Scope();

                    var packet = PacketRegistry.Create(type);
					packet.Deserialize(reader);
					Dispatch(packet);

                    scope.End(packet.GetType().Name, data.Length);

                    PacketTracker.TrackIncoming(new PacketTracker.PacketTrackData
                    {
						packet = packet,
						size = data.Length
                    });
                }
			}
		}

		private static void Dispatch(IPacket packet)
		{
			Profiler.Scope();

			packet.OnDispatched();
		}
	}

}