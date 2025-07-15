using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.Tools.Build
{
    public class BuildCompletePacket : IPacket
    {
        public PacketType Type => PacketType.BuildComplete;

        public int Cell;
        public string PrefabID;
        public Orientation Orientation;
        public List<string> MaterialTags = new List<string>();
        public float Temperature;
        public string FacadeID = "DEFAULT_FACADE";

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Cell);
            writer.Write(PrefabID);
            writer.Write((int)Orientation);
            writer.Write(Temperature);
            writer.Write(FacadeID);

            writer.Write(MaterialTags.Count);
            foreach (var tag in MaterialTags)
                writer.Write(tag);
        }

        public void Deserialize(BinaryReader reader)
        {
            Cell = reader.ReadInt32();
            PrefabID = reader.ReadString();
            Orientation = (Orientation)reader.ReadInt32();
            Temperature = reader.ReadSingle();
            FacadeID = reader.ReadString();

            int count = reader.ReadInt32();
            MaterialTags = new List<string>(count);
            for (int i = 0; i < count; i++)
                MaterialTags.Add(reader.ReadString());
        }

        public void OnDispatched()
        {
            if (!Grid.IsValidCell(Cell))
            {
                DebugConsole.LogWarning($"[BuildCompletePacket] Invalid cell: {Cell}");
                return;
            }

            var def = Assets.GetBuildingDef(PrefabID);
            if (def == null)
            {
                DebugConsole.LogWarning($"[BuildCompletePacket] Unknown building def: {PrefabID}");
                return;
            }

            var tags = MaterialTags.Select(t => new Tag(t)).ToList();

            if (tags.Count == 0)
            {
                DebugConsole.LogWarning($"[BuildCompletePacket] No materials provided for {PrefabID} at cell {Cell}, using SandStone as fallback.");
                tags.Add(SimHashes.SandStone.CreateTag());
            }

            // Destroy ghost/constructable if it still exists
            for (int i = 0; i < (int)Grid.SceneLayer.SceneMAX; i++)
            {
                GameObject obj = Grid.Objects[Cell, i];
                if (obj != null && obj.GetComponent<Constructable>() != null)
                    Util.KDestroyGameObject(obj);
            }

            def.Build(
                Cell,
                Orientation,
                null,
                tags,
                Temperature,
                FacadeID,
                playsound: false,
                GameClock.Instance.GetTime()
            );

            DebugConsole.Log($"[BuildCompletePacket] Finalized {PrefabID} at cell {Cell}");
        }
    }
}
