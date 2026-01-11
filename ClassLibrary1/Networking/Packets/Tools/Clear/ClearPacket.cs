namespace ONI_MP.Networking.Packets.Tools.Clear
{
	public class ClearPacket : DragToolPacket
	{
		public ClearPacket()
		{
			ToolInstance = ClearTool.Instance;
			ToolMode     = DragToolMode.OnDragTool;
		}
	}
}
