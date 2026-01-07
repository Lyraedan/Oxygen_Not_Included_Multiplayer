using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.Tools
{
	internal class EmptyPipePacket : DragToolPacket
	{
		public EmptyPipePacket() : base()
		{
			ToolInstance = EmptyPipeTool.Instance;
			ToolMode = DragToolMode.OnDragTool;
		}
	}
}
