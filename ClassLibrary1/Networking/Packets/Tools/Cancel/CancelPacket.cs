namespace ONI_MP.Networking.Packets.Tools.Cancel
{
	public class CancelPacket : DragToolPacket
	{
		public CancelPacket()
		{
			ToolInstance = CancelTool.Instance;
			ToolMode = DragToolMode.OnDragTool;
		}
	}
}
