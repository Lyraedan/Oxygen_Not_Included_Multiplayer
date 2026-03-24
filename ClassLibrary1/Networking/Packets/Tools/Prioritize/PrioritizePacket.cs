using Shared.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Prioritize
{
    public class PrioritizePacket : DragToolPacket
    {
        public PrioritizePacket()
        {
            using var _ = Profiler.Scope();

            ToolInstance = PrioritizeTool.Instance;
            ToolMode     = DragToolMode.OnDragTool;
        }
    }
}