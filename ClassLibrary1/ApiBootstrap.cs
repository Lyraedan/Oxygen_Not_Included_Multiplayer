using ONI_MP.Api;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP
{
    internal static class ApiBootstrap
    {
        internal static void Initialize()
        {
            MultiplayerApi.PacketSender = new PacketSenderFacade();
        }
    }
}
