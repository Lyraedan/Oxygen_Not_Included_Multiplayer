using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Clear
{
    public enum ClearActionType
    {
        Sweep,
        Mop
    }

    public class ClearPacket : IPacket
    {
        public PacketType Type => PacketType.Clear;

        public List<int> TargetCells = new List<int>();
        public CSteamID SenderId;
        public ClearActionType ActionType;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((int)ActionType); // Write action type
            writer.Write(TargetCells.Count);
            foreach (var cell in TargetCells)
                writer.Write(cell);

            writer.Write(SenderId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            ActionType = (ClearActionType)reader.ReadInt32(); // Read action type
            int count = reader.ReadInt32();
            TargetCells = new List<int>(count);
            for (int i = 0; i < count; i++)
                TargetCells.Add(reader.ReadInt32());

            SenderId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (ActionType == ClearActionType.Sweep)
            {
                foreach (int cell in TargetCells)
                    TrySweepCell(cell);
            }
            else if (ActionType == ClearActionType.Mop)
            {
                foreach (int cell in TargetCells)
                    TryMopCell(cell);
            }

            if (MultiplayerSession.IsHost)
            {
                var exclude = new HashSet<CSteamID>
                {
                    SenderId,
                    MultiplayerSession.LocalSteamID
                };

                PacketSender.SendToAllExcluding(this, exclude);
                DebugConsole.Log($"[ClearPacket] Rebroadcasted {ActionType} to clients (excluding sender {SenderId}) for {TargetCells.Count} cell(s)");
            }
        }

        private void TrySweepCell(int cell)
        {
            if (!Grid.IsValidCell(cell)) return;

            for (int i = 0; i < 45; i++)
            {
                GameObject go = Grid.Objects[cell, i];
                if (go == null) continue;

                TryMarkClearable(go);
            }

            void TryMarkClearable(GameObject target)
            {
                if (!target.TryGetComponent<NetworkIdentity>(out _))
                    return;

                if (target.TryGetComponent(out Clearable clearable))
                {
                    clearable.MarkForClear();
                    DebugConsole.Log($"[ClearPacket] Marked {target.name} at cell for clearing");
                }

                if (target.TryGetComponent(out Pickupable pickup))
                {
                    ObjectLayerListItem item = pickup.objectLayerListItem;
                    while (item != null)
                    {
                        GameObject g2 = item.gameObject;
                        item = item.nextItem;

                        if (g2 == null || g2 == target) continue;
                        if (!g2.TryGetComponent<NetworkIdentity>(out _)) continue;

                        if (g2.TryGetComponent(out Clearable subClearable))
                        {
                            subClearable.MarkForClear();
                            DebugConsole.Log($"[ClearPacket] Marked stacked item {g2.name} for sweeping");
                        }
                    }
                }
            }
        }

        private void TryMopCell(int cell)
        {
            if (!Grid.IsValidCell(cell)) return;

            if (!Grid.Solid[cell] && Grid.Objects[cell, 8] == null && Grid.Element[cell].IsLiquid)
            {
                bool onFloor = Grid.IsValidCell(Grid.CellBelow(cell)) && Grid.Solid[Grid.CellBelow(cell)];
                bool underLimit = Grid.Mass[cell] <= MopTool.maxMopAmt;

                if (onFloor && underLimit)
                {
                    GameObject placer = Util.KInstantiate(Assets.GetPrefab(new Tag("MopPlacer")));
                    Vector3 position = Grid.CellToPosCBC(cell, MopTool.Instance.visualizerLayer);
                    position.z -= 0.15f;
                    placer.transform.SetPosition(position);
                    placer.SetActive(true);
                    Grid.Objects[cell, 8] = placer;

                    var prioritizable = placer.GetComponent<Prioritizable>();
                    if (prioritizable != null)
                        prioritizable.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());

                    DebugConsole.Log($"[ClearPacket] Spawned mop placer at {cell}");
                }
            }
        }
    }
}
