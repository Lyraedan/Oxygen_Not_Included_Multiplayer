using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Interfaces.Networking;
using System;
using System.IO;
using UnityEngine;

public class EntityPositionPacket : IPacket
{
	public int NetId;
	public Vector3 Position;
	public Vector3 Velocity;
	public bool FacingLeft;
	public NavType NavType;
	public float SendInterval;
	public long Timestamp;

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(NetId);
		writer.Write(Position);
		writer.Write(Velocity);
		writer.Write(FacingLeft);
		writer.Write((byte)NavType);
		writer.Write(SendInterval);
		writer.Write(Timestamp);
	}

	public void Deserialize(BinaryReader reader)
	{
		NetId = reader.ReadInt32();
		Position = reader.ReadVector3();
		Velocity = reader.ReadVector3();
		FacingLeft = reader.ReadBoolean();
		NavType = (NavType)reader.ReadByte();
		SendInterval = reader.ReadSingle();
		Timestamp = reader.ReadInt64();
	}

	public void OnDispatched()
	{
		if (MultiplayerSession.IsHost) return;

		if (NetworkIdentityRegistry.TryGet(NetId, out var entity))
		{
			EntityPositionHandler handler = entity.GetComponent<EntityPositionHandler>();
			if (!handler)
				return;

			if (handler.lastPositionTimestamp > Timestamp)
			{
				return; // Recieved out of date position packet, ignore.
			}

            handler.serverPosition = Position;
            handler.serverVelocity = Velocity;
            handler.serverTimestamp = Timestamp;
            handler.lastPositionTimestamp = Timestamp;
            handler.serverFacingLeft = FacingLeft;
        }
		else
		{
			DebugConsole.LogWarning($"[Packets] Could not find entity with NetId {NetId}");
		}
	}
}

