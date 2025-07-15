using System;

namespace ONI_MP.Networking.States
{
    public enum ServerState
    {
        Error = -1,
        Stopped,
        Preparing,
        Starting,
        Started
    }
}
