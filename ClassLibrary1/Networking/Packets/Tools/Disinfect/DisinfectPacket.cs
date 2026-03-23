using Shared.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Disinfect
{
    public class DisinfectPacket : DragToolPacket
    {
        public DisinfectPacket()
        {
            Profiler.Scope();

            ToolInstance = DisinfectTool.Instance;
            ToolMode     = DragToolMode.OnDragTool;
        }
    }
}