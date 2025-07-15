using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Cancel
{
    public class CancelPacket : IPacket
    {
        public PacketType Type => PacketType.Cancel;

        public int Cell;
        public CSteamID SenderId;

        public CancelPacket() { }

        public CancelPacket(int cell, CSteamID senderId)
        {
            Cell = cell;
            SenderId = senderId;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Cell);
            writer.Write(SenderId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            Cell = reader.ReadInt32();
            SenderId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (!Grid.IsValidCell(Cell))
            {
                DebugConsole.LogWarning($"[CancelPacket] Invalid cell: {Cell}");
                return;
            }

            DebugConsole.Log($"[CancelPacket] Cancelling cell: {Cell}");

            // Trigger cancel as if the user clicked it
            MarkForCancel(Cell);

            // If host, rebroadcast
            if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllExcluding(this, new HashSet<CSteamID> { SenderId, MultiplayerSession.LocalSteamID });
                DebugConsole.Log($"[CancelPacket] Host rebroadcasted cancel for cell {Cell}");
            }
        }

        public void MarkForCancel(int cell)
        {
            if (!Grid.IsValidCell(cell))
                return;

            for (int i = 0; i < 45; i++)
            {
                GameObject obj = Grid.Objects[cell, i];
                if (obj != null)
                {
                    string filter = CancelTool.Instance?.GetFilterLayerFromGameObject(obj);
                    if (filter == null || !CancelTool.Instance.IsActiveLayer(filter))
                        continue;

                    obj.Trigger((int) GameHashes.Cancel);
                }
            }
        }
    }
}
