using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.Tools.Mop
{
	public class MopToolPacket : DragToolPacket
	{
		public MopToolPacket()
		{
			Profiler.Active.Scope();

			ToolInstance = MopTool.Instance;
			ToolMode     = DragToolMode.OnDragTool;
		}
	}
}
