using ONI_MP.DebugTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	internal class BulkPacketMonitor : MonoBehaviour, IRender200ms
	{
		public void Render200ms(float dt)
		{
			DebugConsole.Log("BulkPacketMonitor onDispatchingBulkPackets");
			PacketSender.DispatchPendingBulkPackets();
		}
	}
}
