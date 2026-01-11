using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Deconstruct
{
	public class DeconstructPacket : DragToolPacket
	{
		public DeconstructPacket() : base()
		{
			ToolInstance = DeconstructTool.Instance;
			ToolMode = DragToolMode.OnDragTool;
		}
	}
}
