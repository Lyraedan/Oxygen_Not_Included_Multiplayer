namespace ONI_MP.Networking.Packets.Tools.Harvest;

public class HarvestToolPacket : DragToolPacket
{
    public HarvestToolPacket()
    {
        ToolInstance = HarvestTool.Instance;
        ToolMode     = DragToolMode.OnDragTool;
    }
}