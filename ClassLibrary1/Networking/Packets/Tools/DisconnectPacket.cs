using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Clear;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools
{
	internal class DisconnectPacket : DragToolPacket
	{
		public DisconnectPacket() : base() 
		{
			ToolInstance = DisconnectTool.Instance;
			ToolMode = DragToolMode.OnDragComplete;
		}
	}
}
