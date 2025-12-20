using System;
using System.Collections.Generic;
using ONI_MP.Api.Networking.Packets.Architecture;

namespace ONI_MP.Api
{
    public static class MultiplayerApi
    {
        /// <summary>
        /// Uses dud packet sender if core mod is not loaded or multiplayer is not initialized.
        /// </summary>
        public static IPacketSender PacketSender { get; internal set; } = new NullPacketSender();
    }
}
