using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Dig
{
    public class DiggablePacket : IPacket
    {
        public PacketType Type => PacketType.Diggable;

        public int Cell;
        public string SenderId;

        public DiggablePacket() { }

        public DiggablePacket(int cell, string senderId)
        {
            Cell = cell;
            SenderId = senderId;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Cell);
            writer.Write(SenderId);
        }

        public void Deserialize(BinaryReader reader)
        {
            Cell = reader.ReadInt32();
            SenderId = reader.ReadString();
        }

        public void OnDispatched()
        {
            if (!Grid.IsValidCell(Cell))
            {
                DebugConsole.LogWarning($"[DiggablePacket] Invalid cell: {Cell}");
                return;
            }

            if (Diggable.GetDiggable(Cell) != null)
            {
                return;
            }

            // Create diggable object at the given cell
            Vector3 position = Grid.CellToPos(Cell, 0.5f, 0f, 0f);
            GameObject diggableGO = Util.KInstantiate(Assets.GetPrefab(new Tag("DigPlacer")), position);
            diggableGO.SetActive(true);

            // If host, forward to everyone except sender and host
            if (MultiplayerSession.IsHost)
            {
                var excludeSet = new HashSet<string>
                {
                    SenderId,
                    MultiplayerSession.LocalId
                };
                PacketSender.SendToAllExcluding(this, excludeSet);
                DebugConsole.Log($"[DiggablePacket] Host forwarded diggable packet for cell {Cell} to all except sender {SenderId} and self.");
            }
        }
    }
}
