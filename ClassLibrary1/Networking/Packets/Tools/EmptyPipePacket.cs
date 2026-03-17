using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.Tools
{
	internal class EmptyPipePacket : DragToolPacket
	{
		public EmptyPipePacket() : base()
		{
			Profiler.Active.Scope();

			ToolInstance = EmptyPipeTool.Instance;
			ToolMode = DragToolMode.OnDragTool;
		}
	}
}
