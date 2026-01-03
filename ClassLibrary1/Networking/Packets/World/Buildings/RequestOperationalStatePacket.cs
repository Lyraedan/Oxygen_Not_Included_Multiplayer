using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Scripts.Buildings;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace ONI_MP.Networking.Packets.World.Buildings
{
	internal class RequestOperationalStatePacket : IPacket
	{
		public RequestOperationalStatePacket() { }
		public RequestOperationalStatePacket(MonoBehaviour o)
		{
			NetId = o.GetNetId();
		}

		public int NetId;
		public bool IsActive, IsOperational, IsFunctional;
		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
		}

		public void OnDispatched()
		{
			if (!MultiplayerSession.IsHost)
				return;

			if (!NetworkIdentityRegistry.TryGet(NetId, out var entity))
				return;
			if (!entity.TryGetComponent<Operational>(out var server))
				return;

			server.IsOperational = server.IsOperational;
		}
	}
}
