using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Disinfect
{
    public class DisinfectPacket : IPacket
    {
        public PacketType Type => PacketType.Disinfect;

        public int Cell;

        public DisinfectPacket() { }

        public DisinfectPacket(int cell)
        {
            Cell = cell;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Cell);
        }

        public void Deserialize(BinaryReader reader)
        {
            Cell = reader.ReadInt32();
        }

        public void OnDispatched()
        {
            GameObject go = Grid.Objects[Cell, 0];
            if (go != null && go.TryGetComponent(out Disinfectable disinfectable))
            {
                disinfectable.MarkForDisinfect();
            }

            // Rebroadcast this to all clients
            if(MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(this);
            }
        }
    }
}
