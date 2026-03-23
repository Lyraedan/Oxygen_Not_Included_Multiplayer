using Shared.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Harvest;

public class HarvestToolPacket : DragToolPacket
{
    public HarvestToolPacket()
    {
        Profiler.Scope();

        ToolInstance = HarvestTool.Instance;
        ToolMode     = DragToolMode.OnDragTool;
    }
}