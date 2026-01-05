using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
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

            /*
            // Check if this is a duplicant with our client controller
            var clientController = entity.GetComponent<DuplicantClientController>();
			if (clientController != null)
			{
				clientController.OnPositionReceived(Position, Velocity, FacingLeft, NavType);
				return;
			}

			// Fallback for non-duplicant entities: use simple interpolation
			var anim = entity.GetComponent<KBatchedAnimController>();
			if (anim == null)
			{
				DebugConsole.LogWarning($"[Packets] No KBatchedAnimController found on entity {NetId}");
				return;
			}

			entity.StopCoroutine("InterpolateKAnimPosition");
			entity.StartCoroutine(InterpolateKAnimPosition(anim, Position, FacingLeft));*/
        }
		else
		{
			DebugConsole.LogWarning($"[Packets] Could not find entity with NetId {NetId}");
		}
	}

	[Obsolete("Use EntityPositionHandler.UpdatePosition instead")]
	private System.Collections.IEnumerator InterpolateKAnimPosition(KBatchedAnimController anim, Vector3 targetPos, bool facingLeft)
	{
		Vector3 startPos = anim.transform.GetPosition();
		float duration = SendInterval * 1.2f;
		float elapsed = 0f;

		anim.FlipX = facingLeft;

		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = elapsed / duration;
			anim.transform.SetPosition(Vector3.Lerp(startPos, targetPos, t));
			yield return null;
		}

		// Snap at the end to prevent drift
		anim.transform.SetPosition(targetPos);
	}
}

