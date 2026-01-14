using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Interfaces.Networking;
using System;
using System.IO;
using UnityEngine;

public class EntityPositionPacket : IPacket, IBulkablePacket
{
	public int NetId;
	public Vector3 Position;
	public Vector3 Velocity;
	public bool FlipX;
	public bool FlipY;
	public NavType NavType;
	public float SendInterval;
	public long Timestamp;

    public int MaxPackSize => 500;

    public uint IntervalMs => 50;

    public void Serialize(BinaryWriter writer)
	{
		writer.Write(NetId);
		writer.Write(Position);
		writer.Write(Velocity);
		writer.Write(FlipX);
		writer.Write(FlipY);
		writer.Write((byte)NavType);
		writer.Write(SendInterval);
		writer.Write(Timestamp);
	}

	public void Deserialize(BinaryReader reader)
	{
		NetId = reader.ReadInt32();
		Position = reader.ReadVector3();
		Velocity = reader.ReadVector3();
		FlipX = reader.ReadBoolean();
		FlipY = reader.ReadBoolean();
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

            var sample = new EntityPositionHandler.PositionSample
            {
                Position = Position,
                Velocity = Velocity,
                FlipX = FlipX,
                FlipY = FlipY,
                Timestamp = Timestamp
            };
            handler.EnqueuePosition(sample);
        }
		else
		{
			DebugConsole.LogWarning($"[Packets] Could not find entity with NetId {NetId}");
		}
	}
}

