using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;

namespace ONI_MP.Networking.Relay.Steam
{
    public class SteamPacketSender : RelayPacketSender
    {
        public override bool SendToConnection(object conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            HSteamNetConnection s_conn = (HSteamNetConnection)conn;

            var bytes = PacketSender.SerializePacketForSending(packet);
            var _sendType = (int)sendType;

            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);

                var result = SteamNetworkingSockets.SendMessageToConnection(s_conn, unmanagedPointer, (uint)bytes.Length, _sendType, out long msgNum);

                bool sent = result == EResult.k_EResultOK;

                if (!sent)
                {
                    // DebugConsole.LogError($"[Sockets] Failed to send {packet.Type} to conn {conn} ({Utils.FormatBytes(bytes.Length)} | result: {result})", false);
                }
                else
                {
                    PacketTracker.TrackSent(new PacketTracker.PacketTrackData
                    {
                        packet = packet,
                        size = bytes.Length
                    });
                    //DebugConsole.Log($"[Sockets] Sent {packet.Type} to conn {conn} ({Utils.FormatBytes(bytes.Length)})");
                }
                return sent;
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedPointer);
            }
        }
    }
}
