using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Build
{
    public class BuildPacket : IPacket
    {
        public PacketType Type => PacketType.Build;

        public string PrefabID;
        public int Cell;
        public Orientation Orientation;
        public List<string> MaterialTags = new List<string>();
        public CSteamID SenderId;

        public BuildPacket() { }

        public BuildPacket(string prefabID, int cell, Orientation orientation, IEnumerable<Tag> materials, CSteamID senderId)
        {
            PrefabID = prefabID;
            Cell = cell;
            Orientation = orientation;
            MaterialTags = materials.Select(t => t.ToString()).ToList();
            SenderId = senderId;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PrefabID);
            writer.Write(Cell);
            writer.Write((int)Orientation);
            writer.Write(MaterialTags.Count);
            foreach (var tag in MaterialTags)
                writer.Write(tag);
            writer.Write(SenderId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            PrefabID = reader.ReadString();
            Cell = reader.ReadInt32();
            Orientation = (Orientation)reader.ReadInt32();
            int count = reader.ReadInt32();
            MaterialTags = new List<string>();
            for (int i = 0; i < count; i++)
                MaterialTags.Add(reader.ReadString());
            SenderId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (!Grid.IsValidCell(Cell))
            {
                DebugConsole.LogWarning($"[BuildPacket] Invalid cell: {Cell}");
                return;
            }

            var def = Assets.GetBuildingDef(PrefabID);
            if (def == null)
            {
                DebugConsole.LogWarning($"[BuildPacket] Unknown building def: {PrefabID}");
                return;
            }

            var tags = MaterialTags.Select(t => new Tag(t)).ToList();
            Vector3 pos = Grid.CellToPosCBC(Cell, Grid.SceneLayer.Building);

            GameObject visualizer = Util.KInstantiate(def.BuildingPreview, pos);
            def.TryPlace(visualizer, pos, Orientation, tags, "DEFAULT_FACADE");

            // Instant build
            //def.Build(Cell, Orientation, null, tags, temp, "DEFAULT_FACADE", playsound: false, GameClock.Instance.GetTime());

            // Host rebroadcast to other clients
            if (MultiplayerSession.IsHost)
            {
                var exclude = new HashSet<CSteamID> {
                    SenderId,
                    MultiplayerSession.LocalSteamID
                };
                PacketSender.SendToAllExcluding(this, exclude);
                DebugConsole.Log($"[BuildPacket] Host rebroadcasted build for {PrefabID} at {Cell}");
            }
        }

    }
}
