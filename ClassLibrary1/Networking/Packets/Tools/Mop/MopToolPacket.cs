namespace ONI_MP.Networking.Packets.Tools.Mop
{
	public class MopToolPacket : DragToolPacket
	{
		public MopToolPacket()
		{
			ToolInstance = MopTool.Instance;
			ToolMode     = DragToolMode.OnDragTool;
		}
	}
}
