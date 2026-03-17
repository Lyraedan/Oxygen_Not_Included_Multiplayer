using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Clear
{
	public class ClearPacket : DragToolPacket
	{
		public ClearPacket()
		{
			Profiler.Active.Scope();

			ToolInstance = ClearTool.Instance;
			ToolMode     = DragToolMode.OnDragTool;
		}
	}
}
