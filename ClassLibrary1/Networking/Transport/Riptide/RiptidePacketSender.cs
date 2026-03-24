using System;
using Riptide;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using Shared.Profiling;

namespace ONI_MP.Networking.Transport.Lan
{
    public class RiptidePacketSender : TransportPacketSender
    {
        private const int MAX_PAYLOAD_BYTES = 1000;

        public override bool SendToConnection(object conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            using var _ = Profiler.Scope();

            if (conn is not Connection connection)
                return false;

            if (!connection.IsConnected)
                return false;

            byte[] bytes = PacketSender.SerializePacketForSending(packet);

            if (bytes.Length > MAX_PAYLOAD_BYTES && packet is not ChunkedPacket)
            {
                return SendChunked(connection, bytes, sendType);
            }

            return SendRaw(connection, bytes, packet, sendType);
        }

        private bool SendRaw(Connection connection, byte[] bytes, IPacket packet, SteamNetworkingSend sendType)
        {
            MessageSendMode sendMode = ConvertSendType(sendType);
            Riptide.Message msg = Riptide.Message.Create(sendMode, 1);
            msg.AddBytes(bytes);

            if (MultiplayerSession.IsHost)
            {
                var server = RiptideServer.ServerInstance;
                if (server == null)
                    return false;

                server.Send(msg, connection);
            }
            else
            {
                var client = RiptideClient.Client;
                if (client == null)
                    return false;

                client.Send(msg);
            }

            PacketTracker.TrackSent(new PacketTracker.PacketTrackData
            {
                packet = packet,
                size = bytes.Length
            });
            return true;
        }

        private bool SendChunked(Connection connection, byte[] fullData, SteamNetworkingSend sendType)
        {
            int chunkDataSize = MAX_PAYLOAD_BYTES - 20; // overhead for ChunkedPacket header
            int totalChunks = (fullData.Length + chunkDataSize - 1) / chunkDataSize;
            int sequenceId = ChunkedPacket.GetNextSequenceId();

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * chunkDataSize;
                int length = Math.Min(chunkDataSize, fullData.Length - offset);
                byte[] chunkData = new byte[length];
                Array.Copy(fullData, offset, chunkData, 0, length);

                var chunk = new ChunkedPacket
                {
                    SequenceId = sequenceId,
                    ChunkIndex = i,
                    TotalChunks = totalChunks,
                    ChunkData = chunkData
                };

                byte[] chunkBytes = PacketSender.SerializePacketForSending(chunk);
                SendRaw(connection, chunkBytes, chunk, sendType);
            }

            return true;
        }

        private static MessageSendMode ConvertSendType(SteamNetworkingSend sendType)
        {
            using var _ = Profiler.Scope();

            switch (sendType)
            {
                case SteamNetworkingSend.Reliable:
                case SteamNetworkingSend.ReliableNoNagle:
                    return MessageSendMode.Reliable;

                case SteamNetworkingSend.Unreliable:
                case SteamNetworkingSend.UnreliableNoNagle:
                case SteamNetworkingSend.UnreliableNoDelay:
                    return MessageSendMode.Unreliable;

                default:
                    // Catch-all for unexpected flag combinations
                    if ((sendType & SteamNetworkingSend.Reliable) != 0)
                        return MessageSendMode.Reliable;

                    return MessageSendMode.Unreliable;
            }
        }
    }
}
