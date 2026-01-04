using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Animation
{
	internal class StandardWorker_WorkingState_Packet : IPacket
	{
		public StandardWorker_WorkingState_Packet() { }

		public StandardWorker_WorkingState_Packet(StandardWorker worker, Workable workable, bool startedWorking)
		{
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
			if (MultiplayerSession.IsHost)
				return;

			if (!NetworkIdentityRegistry.TryGetComponent<StandardWorker>(WorkerNetId, out var worker))
				return;
			GameObject workableGO = null;
			if (StartingToWork)
			{
				if (!NetworkIdentityRegistry.TryGetComponent<Workable>(WorkerNetId, out var protoWorkable))
					return;
				workableGO = protoWorkable.gameObject;

				var targetWorkableCmp = workableGO.GetComponent(WorkableType);
				if (targetWorkableCmp == null || targetWorkableCmp is not Workable workable)
				{
					DebugConsole.LogWarning("Could not find workable of type " + WorkableType + " on " + workableGO.GetProperName());
					return;
				}
				worker.StartWork(new(workable));
			}
			else
				worker.StopWork();

			DebugConsole.Log("[StandardWorker_WorkingState_Packet] workable change triggered for " + worker.name + ": " + (StartingToWork ? "Started working on " + workableGO.name : "stopped working"));
		}
	}
}
