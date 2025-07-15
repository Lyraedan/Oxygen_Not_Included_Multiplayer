using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Prioritize
{
    public class PrioritizePacket : IPacket
    {
        public PacketType Type => PacketType.Prioritize;

        public List<int> TargetCells = new List<int>();
        public PrioritySetting Priority;
        public CSteamID SenderId;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TargetCells.Count);
            foreach (var cell in TargetCells)
                writer.Write(cell);

            writer.Write((int)Priority.priority_class);
            writer.Write(Priority.priority_value);

            writer.Write(SenderId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            TargetCells = new List<int>(count);
            for (int i = 0; i < count; i++)
                TargetCells.Add(reader.ReadInt32());

            Priority = new PrioritySetting(
                (PriorityScreen.PriorityClass)reader.ReadInt32(),
                reader.ReadInt32()
            );

            SenderId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            foreach (int cell in TargetCells)
            {
                if (!Grid.IsValidCell(cell)) continue;

                for (int i = 0; i < 45; i++)
                {
                    GameObject go = Grid.Objects[cell, i];
                    if (go == null) continue;

                    TryApplyPriority(go);
                }
            }

            void TryApplyPriority(GameObject target)
            {
                var prio = target.GetComponent<Prioritizable>();
                if (prio != null && prio.showIcon && prio.IsPrioritizable())
                {
                    prio.SetMasterPriority(Priority);
                    DebugConsole.Log($"[PrioritizePacket] Applied priority {Priority.priority_class}:{Priority.priority_value} to {target.name}");
                }

                // Handle items in pickup stacks
                if (target.TryGetComponent(out Pickupable pickup))
                {
                    ObjectLayerListItem item = pickup.objectLayerListItem;
                    while (item != null)
                    {
                        GameObject g2 = item.gameObject;
                        item = item.nextItem;

                        if (g2 == null || g2.GetComponent<MinionIdentity>() != null) continue;

                        var subprio = g2.GetComponent<Prioritizable>();
                        if (subprio != null && subprio.showIcon && subprio.IsPrioritizable())
                        {
                            subprio.SetMasterPriority(Priority);
                            DebugConsole.Log($"[PrioritizePacket] Applied priority to item in stack: {g2.name}");
                        }
                    }
                }
            }

            if (MultiplayerSession.IsHost)
            {
                var exclude = new HashSet<CSteamID>
                {
                    SenderId,
                    MultiplayerSession.LocalSteamID
                };
                PacketSender.SendToAllExcluding(this, exclude);
                DebugConsole.Log($"[PrioritizePacket] Rebroadcasted to clients (excluding sender {SenderId}) for {TargetCells.Count} cell(s)");
            }
        }
    }
}
