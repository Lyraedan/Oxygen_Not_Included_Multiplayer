using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Packets.Tools.Deconstruct
{
    public class DeconstructPacket : IPacket
    {
        public PacketType Type => PacketType.Deconstruct;

        public int Cell;
        public CSteamID SenderId;

        public DeconstructPacket() { }

        public DeconstructPacket(int cell, CSteamID senderId)
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
                DebugConsole.LogWarning($"[DeconstructPacket] Invalid cell: {Cell}");
                return;
            }

            for (int i = 0; i < 45; i++)
            {
                GameObject go = Grid.Objects[Cell, i];
                if (go == null)
                    continue;

                var deconstructable = go.GetComponent<Deconstructable>();
                if (deconstructable != null && deconstructable.allowDeconstruction)
                {
                    deconstructable.QueueDeconstruction(userTriggered: true);
                }
            }

            if (MultiplayerSession.IsHost)
            {
                var exclude = new HashSet<CSteamID> { SenderId, MultiplayerSession.LocalSteamID };
                PacketSender.SendToAllExcluding(this, exclude);
                DebugConsole.Log($"[DeconstructPacket] Host rebroadcasted deconstruct at cell {Cell}");
            }
        }
    }
}
