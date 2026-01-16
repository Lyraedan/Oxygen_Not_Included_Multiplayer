using ONI_MP.Misc;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Core
{
	public class PlayerCursorPacket : IPacket
	{
		public ulong SteamID;
		public Vector3 Position;
		public Color Color;
		public CursorState CursorState;

		// Viewport for targeted sync
		public int ViewMinX, ViewMinY, ViewMaxX, ViewMaxY;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(SteamID);
			writer.Write(Position);
			writer.Write(Color.r);
			writer.Write(Color.g);
			writer.Write(Color.b);
			writer.Write(Color.a);
			writer.Write((int)CursorState);
			writer.Write(ViewMinX);
			writer.Write(ViewMinY);
			writer.Write(ViewMaxX);
			writer.Write(ViewMaxY);
		}

		public void Deserialize(BinaryReader reader)
		{
			SteamID = reader.ReadUInt64();
			Position = reader.ReadVector3();
			float r = reader.ReadSingle();
			float g = reader.ReadSingle();
			float b = reader.ReadSingle();
			float a = reader.ReadSingle();
			Color = new Color(r, g, b, a);
			CursorState = (CursorState)reader.ReadInt32();
			ViewMinX = reader.ReadInt32();
			ViewMinY = reader.ReadInt32();
			ViewMaxX = reader.ReadInt32();
			ViewMaxY = reader.ReadInt32();
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.TryGetCursorObject(SteamID, out var cursorGO))
			{
				var cursorComponent = cursorGO.GetComponent<PlayerCursor>();
				if (cursorComponent != null)
				{
					cursorComponent.SetState(CursorState);
					cursorComponent.SetColor(Color);
					cursorComponent.SetVisibility(true);
					cursorComponent.StopCoroutine("InterpolateCursorPosition");
					cursorComponent.StartCoroutine(InterpolateCursorPosition(cursorComponent.transform, Position));
				}
			}
			else
			{
				if (Utils.IsInGame())
				{
					MultiplayerSession.CreateNewPlayerCursor(SteamID); // Create a cursor if one doesn't exist.
				}
			}


			// Forward to others if host
			if (MultiplayerSession.IsHost)
			{
				// Update Viewport in Syncer
				if (WorldStateSyncer.Instance != null)
				{
					WorldStateSyncer.Instance.UpdateClientView(SteamID, ViewMinX, ViewMinY, ViewMaxX, ViewMaxY);
				}

				PacketSender.SendToAllOtherPeers(this);
			}
		}

		private IEnumerator InterpolateCursorPosition(Transform target, Vector3 targetPos)
		{
			Vector3 start = target.position;
			float duration = CursorManager.SendInterval;
			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = elapsed / duration;
				target.position = Vector3.Lerp(start, targetPos, t);
				yield return null;
			}

			target.position = targetPos;
		}

	}
}
