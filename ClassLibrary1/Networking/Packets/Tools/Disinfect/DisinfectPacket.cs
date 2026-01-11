namespace ONI_MP.Networking.Packets.Tools.Disinfect
{
    public class DisinfectPacket : DragToolPacket
    {
        public DisinfectPacket()
        {
            ToolInstance = DisinfectTool.Instance;
            ToolMode     = DragToolMode.OnDragTool;
        }
    }
}