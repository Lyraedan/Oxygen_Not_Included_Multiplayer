using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;

namespace ONI_MP.Networking.Relay.Platforms.Steam
{
    public class SteamConnection : INetworkConnection
    {
        public CSteamID SteamID { get; }
        public HSteamNetConnection Handle { get; }

        public object RawHandle => Handle;

        public SteamConnection(CSteamID id, HSteamNetConnection handle)
        {
            SteamID = id;
            Handle = handle;
        }

        public bool IsValid => SteamID.IsValid();
        public string DebugName => $"Steam:{SteamID.ToString()}";

        public string Id => SteamID.ToString();

        public void Send(IPacket packet, SendType sendType)
        {
            var bytes = PacketSender.SerializePacket(packet);
            int steamSendType = sendType == SendType.Reliable ? (int)SteamNetworkingSend.Reliable : (int)SteamNetworkingSend.Unreliable;

            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                var result = SteamNetworkingSockets.SendMessageToConnection(
                    Handle, ptr, (uint)bytes.Length, steamSendType, out _);

                if (result != EResult.k_EResultOK)
                {
                    DebugConsole.LogError($"[SteamConnection] Failed to send {packet.Type} to {DebugName}: {result}");
                } else
                {
                    DebugConsole.Log($"[Send] Type={packet.Type}, Size={bytes.Length}, To={SteamID} (self={SteamID == SteamUser.GetSteamID()})");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
