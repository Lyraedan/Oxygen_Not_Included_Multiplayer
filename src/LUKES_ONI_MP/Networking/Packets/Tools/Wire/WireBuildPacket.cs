using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Wire
{
    public class WireBuildPacket : IPacket
    {
        public PacketType Type => PacketType.WireBuild;

        public struct Node
        {
            public int Cell;
            public bool Valid;

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Cell);
                writer.Write(Valid);
            }

            public static Node Deserialize(BinaryReader reader)
            {
                Node node;
                node.Cell = reader.ReadInt32();
                node.Valid = reader.ReadBoolean();
                return node;
            }
        }

        public List<Node> Path = new List<Node>();
        public CSteamID SenderId;

        public WireBuildPacket() { }

        public WireBuildPacket(List<Node> path, CSteamID senderId)
        {
            Path = path;
            SenderId = senderId;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((ushort)Path.Count);
            foreach (var node in Path)
                node.Serialize(writer);

            writer.Write(SenderId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            Path.Clear();
            ushort count = reader.ReadUInt16();
            for (int i = 0; i < count; i++)
                Path.Add(Node.Deserialize(reader));

            SenderId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (Path.Count == 0)
                return;

            var conduitMgr = Game.Instance.electricalConduitSystem;
            var def = Assets.GetBuildingDef("WireRefined"); // Replace with correct wire ID if needed

            if (def == null)
            {
                DebugConsole.LogError("[WireBuildPacket] Could not find BuildingDef for 'WireRefined'");
                return;
            }

            // Optional: Preview visualizers (can be removed if you want only final placement)
            for (int i = 0; i < Path.Count; i++)
            {
                var node = Path[i];
                if (!node.Valid)
                    continue;

                Vector3 pos = Grid.CellToPosCBC(node.Cell, def.SceneLayer);
                GameObject vis = Object.Instantiate(def.BuildingPreview, pos, Quaternion.identity);
                vis.SetActive(true);

                var anim = vis.GetComponent<KBatchedAnimController>();
                if (anim != null)
                {
                    anim.TintColour = Color.white;
                    anim.Play("None_Place");
                }
            }

            // Add connections + track updated cells
            HashSet<int> updatedCells = new HashSet<int>();

            for (int i = 1; i < Path.Count; i++)
            {
                var a = Path[i - 1];
                var b = Path[i];

                if (!a.Valid || !b.Valid)
                    continue;

                var conn = UtilityConnectionsExtensions.DirectionFromToCell(a.Cell, b.Cell);
                if (conn == 0)
                    continue;

                conduitMgr.AddConnection(conn, a.Cell, false);
                conduitMgr.AddConnection(conn.InverseDirection(), b.Cell, false);

                updatedCells.Add(a.Cell);
                updatedCells.Add(b.Cell);
            }

            // Force connection visuals to update
            foreach (int cell in updatedCells)
            {
                var conn = conduitMgr.GetConnections(cell, false);
                conduitMgr.SetConnections(conn, cell, false);
            }

            // Rebuild network if needed
            //conduitMgr.ForceRebuildNetworks();

            // Re-broadcast from host to all clients except sender
            if (MultiplayerSession.IsHost)
            {
                var exclude = new HashSet<CSteamID> { SenderId, MultiplayerSession.LocalSteamID };
                PacketSender.SendToAllExcluding(this, exclude);
            }
        }
    }
}
