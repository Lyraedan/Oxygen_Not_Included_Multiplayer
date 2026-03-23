using ONI_MP.DebugTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Profiling;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	internal class BulkPacketMonitor : MonoBehaviour
	{
		private float updateIntervalSeconds = 0.2f;
		private float updateTimer;

		public void LateUpdate()
		{
			Profiler.Scope();

			if (!MultiplayerSession.InSession)
				return;

			updateTimer += Time.unscaledDeltaTime;

			if (updateTimer < updateIntervalSeconds)
			{
				return;
			}
			updateTimer -= updateIntervalSeconds;
			PacketSender.DispatchPendingBulkPackets();
		}
	}
}
