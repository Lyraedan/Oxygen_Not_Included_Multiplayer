using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Disinfect
{
    public class DisinfectPacket : DragToolPacket
    {
        public DisinfectPacket()
        {
            Profiler.Active.Scope();

            ToolInstance = DisinfectTool.Instance;
            ToolMode     = DragToolMode.OnDragTool;
        }
    }
}