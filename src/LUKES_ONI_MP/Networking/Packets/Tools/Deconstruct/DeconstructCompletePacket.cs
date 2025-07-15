using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Deconstruct
{
    public class DeconstructCompletePacket : IPacket
    {
        public PacketType Type => PacketType.DeconstructComplete;

        public int Cell;

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
            if (!Grid.IsValidCell(Cell))
                return;

            for (int i = 0; i < 45; i++)
            {
                GameObject go = Grid.Objects[Cell, i];
                if (go == null)
                    continue;

                var deconstructable = go.GetComponent<Deconstructable>();
                if (deconstructable != null && !deconstructable.HasBeenDestroyed)
                {
                    DebugConsole.Log($"[DeconstructCompletePacket] Forcing deconstruct at cell {Cell} on client.");
                    deconstructable.ForceDestroyAndGetMaterials();
                }
            }
        }
    }
}
