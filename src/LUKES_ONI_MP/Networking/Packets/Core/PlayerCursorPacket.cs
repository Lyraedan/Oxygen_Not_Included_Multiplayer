using System.Collections;
using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Core
{
    public class PlayerCursorPacket : IPacket
    {
        public CSteamID SteamID;
        public Vector3 Position;
        public Color Color;
        public CursorState CursorState;

        public PacketType Type => PacketType.PlayerCursor;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SteamID.m_SteamID);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            writer.Write(Color.r);
            writer.Write(Color.g);
            writer.Write(Color.b);
            writer.Write(Color.a);
            writer.Write((int) CursorState);
        }

        public void Deserialize(BinaryReader reader)
        {
            SteamID = new CSteamID(reader.ReadUInt64());
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            Position = new Vector3(x, y, z);
            float r = reader.ReadSingle();
            float g = reader.ReadSingle();
            float b = reader.ReadSingle();
            float a = reader.ReadSingle();
            Color = new Color(r, g, b, a);
            CursorState = (CursorState)reader.ReadInt32();
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
                HashSet<CSteamID> excluding = new HashSet<CSteamID>
                {
                    SteamID,
                    MultiplayerSession.LocalSteamID
                };
                PacketSender.SendToAllExcluding(this, excluding);
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
