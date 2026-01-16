using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LanPacketSender : RelayPacketSender
    {
        private UdpClient udpClient;

        public override bool SendToConnection(object conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.ReliableNoNagle)
        {
            if (conn is not IPEndPoint)
                return false;

            if (udpClient == null)
                return false;

            var bytes = PacketSender.SerializePacketForSending(packet);
            var _sendType = (int)sendType;

            try
            {
                udpClient.Send(bytes, bytes.Length);
                return true;
            }
            catch(Exception e)
            {
                DebugConsole.LogError($"[LanPacketSender] UDP send failed: {e}", false);
                return false;
            }
        }

        // TODO since this won't use Steam the SteamNetworkingSend will need converting to the correct equivilent sendtype here
    }
}
