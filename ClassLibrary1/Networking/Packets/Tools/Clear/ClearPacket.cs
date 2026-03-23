using Shared.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Clear
{
	public class ClearPacket : DragToolPacket
	{
		public ClearPacket()
		{
			Profiler.Scope();

			ToolInstance = ClearTool.Instance;
			ToolMode     = DragToolMode.OnDragTool;
		}
	}
}
