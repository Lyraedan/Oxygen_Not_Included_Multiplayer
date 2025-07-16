using System;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;

namespace ONI_MP.Networking.Relay.Platforms.EOS
{
    public class EosConnection : INetworkConnection
    {
        public ProductUserId RemoteUserId { get; }

        public string Id => RemoteUserId.ToString();
        public string DebugName => $"EOS:{Id}";
        public object RawHandle => RemoteUserId;

        public bool IsValid => RemoteUserId != null && RemoteUserId.IsValid();

        public EosConnection(ProductUserId remoteUserId)
        {
            RemoteUserId = remoteUserId;
        }

        public void Send(IPacket packet, SendType sendType = SendType.Reliable)
        {
            if (!IsValid)
            {
                DebugConsole.LogError($"[EosConnection] Invalid connection for {DebugName}");
                return;
            }

            byte[] data = PacketSender.SerializePacket(packet);
            if (data == null || data.Length == 0)
            {
                DebugConsole.LogError("[EosConnection] Tried to send null or empty packet.");
                return;
            }

            var options = new SendPacketOptions
            {
                LocalUserId = EOSPlatform.LocalUserId,
                RemoteUserId = RemoteUserId,
                SocketId = new SocketId { SocketName = "ONI_MP" },
                Channel = 0,
                Data = data,
                Reliability = sendType == SendType.Reliable
                    ? PacketReliability.ReliableOrdered
                    : PacketReliability.UnreliableUnordered
            };

            var result = EOSPlatform.P2P.SendPacket(options);

            if (result != Epic.OnlineServices.Result.Success)
            {
                DebugConsole.LogError($"[EosConnection] Failed to send packet to {Id}: {result}");
            }
        }
    }
}
