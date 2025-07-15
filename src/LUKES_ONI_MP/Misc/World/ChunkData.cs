using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Misc.World
{
    public class ChunkData
    {
        public int TileX, TileY, Width, Height;
        public ushort[] Tiles;
        public float[] Temperatures, Masses;
        public byte[] DiseaseIdx;
        public int[] DiseaseCount;

        public void Serialize(BinaryWriter w)
        {
            w.Write(TileX); w.Write(TileY);
            w.Write(Width); w.Write(Height);
            int len = Width * Height;
            w.Write(len);
            for (int i = 0; i < len; i++)
            {
                w.Write(Tiles[i]);
                w.Write(Temperatures[i]);
                w.Write(Masses[i]);
                w.Write(DiseaseIdx[i]);
                w.Write(DiseaseCount[i]);
            }
        }

        public void Deserialize(BinaryReader r)
        {
            TileX = r.ReadInt32(); TileY = r.ReadInt32();
            Width = r.ReadInt32(); Height = r.ReadInt32();
            int len = r.ReadInt32();
            Tiles = new ushort[len];
            Temperatures = new float[len];
            Masses = new float[len];
            DiseaseIdx = new byte[len];
            DiseaseCount = new int[len];
            for (int i = 0; i < len; i++)
            {
                Tiles[i] = r.ReadUInt16();
                Temperatures[i] = r.ReadSingle();
                Masses[i] = r.ReadSingle();
                DiseaseIdx[i] = r.ReadByte();
                DiseaseCount[i] = r.ReadInt32();
            }
        }

        public void Apply()
        {
            int len = Width * Height;
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    int idx = i + j * Width;
                    int x = TileX + i, y = TileY + j;
                    int cell = Grid.XYToCell(x, y);
                    if (!Grid.IsValidCell(cell)) continue;

                    SimMessages.ModifyCell(
                        cell,
                        Tiles[idx],
                        Temperatures[idx],
                        Masses[idx],
                        DiseaseIdx[idx],
                        DiseaseCount[idx],
                        SimMessages.ReplaceType.Replace
                    );
                }
        }
    }


}
