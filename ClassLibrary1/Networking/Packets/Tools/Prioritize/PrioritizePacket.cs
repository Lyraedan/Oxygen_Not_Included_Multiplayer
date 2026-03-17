using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Prioritize
{
    public class PrioritizePacket : DragToolPacket
    {
        public PrioritizePacket()
        {
            Profiler.Active.Scope();

            ToolInstance = PrioritizeTool.Instance;
            ToolMode     = DragToolMode.OnDragTool;
        }
    }
}