using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Cancel
{
	public class CancelPacket : DragToolPacket
	{
		public CancelPacket()
		{
			Profiler.Active.Scope();

			ToolInstance = CancelTool.Instance;
			ToolMode = DragToolMode.OnDragTool;
		}
	}
}
