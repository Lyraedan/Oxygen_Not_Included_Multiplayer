using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Build
{
	public class UtilityBuildPacket : IPacket
	{
		// Actually, let's stick to the existing enum if we can, or add a new one.
		// If I can't edit the enum easily without breaking things (I can, it's my code), I will add UtilityBuild.
		// For now, let's assume I'll add UtilityBuild to PacketType.

		public string PrefabID;
		public CSteamID SenderId;

		public struct Node
		{
			public int Cell;
			public bool Valid;
			// Connection direction flags - indicate which neighbors this segment connects to
			public bool ConnectsUp;
			public bool ConnectsDown;
			public bool ConnectsLeft;
			public bool ConnectsRight;

			public void Serialize(BinaryWriter writer)
			{
				writer.Write(Cell);
				writer.Write(Valid);
				writer.Write(ConnectsUp);
				writer.Write(ConnectsDown);
				writer.Write(ConnectsLeft);
				writer.Write(ConnectsRight);
			}

			public static Node Deserialize(BinaryReader reader)
			{
				return new Node
				{
					Cell = reader.ReadInt32(),
					Valid = reader.ReadBoolean(),
					ConnectsUp = reader.ReadBoolean(),
					ConnectsDown = reader.ReadBoolean(),
					ConnectsLeft = reader.ReadBoolean(),
					ConnectsRight = reader.ReadBoolean()
				};
			}
		}

		public List<Node> Path = new List<Node>();

		public UtilityBuildPacket() { }

		public UtilityBuildPacket(string prefabId, List<Node> path, CSteamID senderId)
		{
			PrefabID = prefabId;
			Path = path;
			SenderId = senderId;
		}

		public List<string> MaterialTags = new List<string>();

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(PrefabID);
			writer.Write(SenderId.m_SteamID);
			writer.Write(Path.Count);
			foreach (var node in Path)
			{
				node.Serialize(writer);
			}
			writer.Write(MaterialTags.Count);
			foreach (var tag in MaterialTags)
			{
				writer.Write(tag);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			PrefabID = reader.ReadString();
			SenderId = new CSteamID(reader.ReadUInt64());
			int count = reader.ReadInt32();
			Path = new List<Node>();
			for (int i = 0; i < count; i++)
			{
				Path.Add(Node.Deserialize(reader));
			}
			int matCount = reader.ReadInt32();
			MaterialTags = new List<string>(matCount);
			for (int i = 0; i < matCount; i++)
			{
				MaterialTags.Add(reader.ReadString());
			}
		}

		public void OnDispatched()
		{
			if (Path.Count == 0) return;

			var def = Assets.GetBuildingDef(PrefabID);
			if (def == null)
			{
				DebugConsole.LogError($"[UtilityBuildPacket] Unknown PrefabID: {PrefabID}");
				return;
			}

			var tags = MaterialTags.Select(t => new Tag(t)).ToList();
			if (tags.Count == 0)
			{
				tags.Add(SimHashes.Copper.CreateTag());
			}

			// Place construction sites
			int placedCount = 0;
			foreach (var node in Path)
			{
				if (!node.Valid) continue;
				int cell = node.Cell;
				if (!Grid.IsValidCell(cell)) continue;

				try
				{
					var existingObj = Grid.Objects[cell, (int)def.ObjectLayer];
					if (existingObj != null) continue;

					Vector3 pos = Grid.CellToPosCBC(cell, def.SceneLayer);
					var go = def.TryPlace(null, pos, Orientation.Neutral, tags, "DEFAULT_FACADE");
					if (go != null)
					{
						placedCount++;

						// Set connection state using our connection flags
						var tileVis = go.GetComponent<KAnimGraphTileVisualizer>();
						if (tileVis != null)
						{
							// Build the UtilityConnections bitmask: Left=1, Right=2, Up=4, Down=8
							UtilityConnections newConnections = (UtilityConnections)0;
							if (node.ConnectsLeft) newConnections |= UtilityConnections.Left;
							if (node.ConnectsRight) newConnections |= UtilityConnections.Right;
							if (node.ConnectsUp) newConnections |= UtilityConnections.Up;
							if (node.ConnectsDown) newConnections |= UtilityConnections.Down;

							tileVis.Connections = newConnections;
							tileVis.Refresh();
						}
					}
				}
				catch (System.Exception e)
				{
					DebugConsole.LogError($"[UtilityBuildPacket] Failed at cell {cell}: {e.Message}");
				}
			}

			// Rebroadcast if Host
			if (MultiplayerSession.IsHost)
			{
				var exclude = new HashSet<CSteamID> { SenderId, MultiplayerSession.LocalSteamID };
				PacketSender.SendToAllExcluding(this, exclude);
			}
		}
	}
}
