using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Move
{
    public class MoveToLocationPacket : IPacket
    {
        public PacketType Type => PacketType.MoveToLocation;

        public int Cell;
        public int TargetNetId;
        public CSteamID SenderId;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Cell);
            writer.Write(TargetNetId);
            writer.Write(SenderId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            Cell = reader.ReadInt32();
            TargetNetId = reader.ReadInt32();
            SenderId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (!MultiplayerSession.IsHost)
                return;

            if (!Grid.IsValidCell(Cell))
            {
                DebugConsole.LogWarning($"[MoveToLocationPacket] Invalid cell: {Cell}");
                return;
            }

            if (!NetworkIdentityRegistry.TryGet(TargetNetId, out var go))
            {
                DebugConsole.LogWarning($"[MoveToLocationPacket] Unknown NetId: {TargetNetId}");
                return;
            }

            if (go.TryGetComponent(out Navigator nav))
            {
                nav.GetSMI<MoveToLocationMonitor.Instance>()?.MoveToLocation(Cell);
                DebugConsole.Log($"[Host] Navigator moved to {Cell} for NetId {TargetNetId}");
            }
            else if (go.TryGetComponent(out Movable movable))
            {
                movable.MoveToLocation(Cell);
                DebugConsole.Log($"[Host] Movable moved to {Cell} for NetId {TargetNetId}");
            }
            else
            {
                DebugConsole.LogWarning($"[MoveToLocationPacket] No Navigator/Movable found on entity {TargetNetId}");
            }
        }
    }
}
