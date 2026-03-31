using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Profiling;
using UnityEngine;
using static RancherChore;

namespace ONI_MP.Networking.Packets.Animation
{
	internal class StandardWorker_WorkingState_Packet : IPacket
	{
		public StandardWorker_WorkingState_Packet() { }

		public StandardWorker_WorkingState_Packet(StandardWorker worker, Workable workable, bool startedWorking)
		{
			using var _ = Profiler.Scope();

			WorkerNetId = worker.GetNetId();
			StartingToWork = startedWorking;
			if (startedWorking)
			{
				WorkableNetId = workable.GetNetId();
				WorkableType = workable.GetType().AssemblyQualifiedName;
			}
		}

		int WorkerNetId, WorkableNetId;
		string WorkableType;
		bool StartingToWork;

		public void Serialize(BinaryWriter writer)
		{
			using var _ = Profiler.Scope();

			writer.Write(WorkerNetId);
			writer.Write(StartingToWork);
			if (StartingToWork)
			{
				writer.Write(WorkableNetId);
				writer.Write(WorkableType);
			}
		}
		public void Deserialize(BinaryReader reader)
		{
			using var _ = Profiler.Scope();

			WorkerNetId = reader.ReadInt32();
			StartingToWork = reader.ReadBoolean();
			if (StartingToWork)
			{
				WorkableNetId = reader.ReadInt32();
				WorkableType = reader.ReadString();
			}
		}

		public void OnDispatched()
		{
			using var _ = Profiler.Scope();

			if (MultiplayerSession.IsHost)
				return;

			if (!NetworkIdentityRegistry.TryGetComponent<StandardWorker>(WorkerNetId, out var worker))
				return;

			GameObject workableGO = null;
			if (StartingToWork)
			{
				if (!NetworkIdentityRegistry.TryGetComponent<Workable>(WorkableNetId, out var protoWorkable))
					return;

				workableGO = protoWorkable.gameObject;

				var workableType = AccessTools.TypeByName(WorkableType);
				if(workableType == null)
				{
					DebugConsole.LogWarning("Could not find workable type " + WorkableType);
				}

				var targetWorkableCmp = workableGO.GetComponent(workableType);
				if (targetWorkableCmp == null || targetWorkableCmp is not Workable workable)
				{
					DebugConsole.LogWarning("Could not find workable of type " + WorkableType + " on " + workableGO.GetProperName());
					return;
				}

				try
				{
					if (!worker.state.Equals(StandardWorker.State.Idle))
					{
						worker.StopWork();
					}
					worker.StartWork(new(workable));
				}
				catch (System.Exception ex)
				{
					DebugConsole.LogWarning($"[StandardWorker_WorkingState_Packet] StartWork failed for {worker.name} on {workableGO.name}: {ex.GetType().Name}");
				}
            }
			else
				worker.StopWork();

			DebugConsole.Log("[StandardWorker_WorkingState_Packet] workable change triggered for " + worker.name + ": " + (StartingToWork ? "Started working on " + workableGO.name : "stopped working"));
		}
	}
}
