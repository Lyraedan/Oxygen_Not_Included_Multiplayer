using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Harvest;

public class HarvestToolPacket : DragToolPacket
{
    public HarvestToolPacket()
    {
        Profiler.Active.Scope();

        ToolInstance = HarvestTool.Instance;
        ToolMode     = DragToolMode.OnDragTool;
    }
}