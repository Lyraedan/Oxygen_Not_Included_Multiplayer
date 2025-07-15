using System;

namespace ONI_MP.Networking.States
{
    public enum ClientState
    {
        Error = -1,
        Disconnected,
        Connecting,
        Connected,
        LoadingWorld,
        InGame
    }

    [Flags]
    public enum ClientReadyState
    {
        Ready,
        Unready
    }
}
