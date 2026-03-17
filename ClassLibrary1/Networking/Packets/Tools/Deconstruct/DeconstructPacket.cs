using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using ONI_MP.Profiling;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Deconstruct
{
	public class DeconstructPacket : DragToolPacket
	{
		public DeconstructPacket() : base()
		{
			Profiler.Active.Scope();

			ToolInstance = DeconstructTool.Instance;
			ToolMode = DragToolMode.OnDragTool;
		}
	}
}
