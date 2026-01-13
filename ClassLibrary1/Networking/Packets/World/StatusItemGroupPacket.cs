using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Interfaces.Networking;

namespace ONI_MP.Networking.Packets.World
{
    public class StatusItemGroupPacket : IPacket, IBulkablePacket
    {
        public enum ItemGroupPacketAction
        {
            Add,
            Remove
        }

        public int MaxPackSize => 500;

        public uint IntervalMs => 50;

        public int NetId;
        public ItemGroupPacketAction Action;

        public string StatusItemId;
        public bool Immediate;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetId);
            writer.Write((int)Action);
            writer.Write(StatusItemId);
            switch (Action)
            {
                case ItemGroupPacketAction.Remove:
                    writer.Write(Immediate);
                    break;
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            NetId = reader.ReadInt32();
            Action = (ItemGroupPacketAction)reader.ReadInt32();
            StatusItemId = reader.ReadString() ?? string.Empty;
            switch(Action)
            {
                case ItemGroupPacketAction.Remove:
                    Immediate = reader.ReadBoolean();
                    break;
            }
        }

        public void OnDispatched()
        {
            if (!NetworkIdentityRegistry.TryGet(NetId, out NetworkIdentity identity))
            {
                DebugConsole.LogWarning($"[StatusItemGroupPacket] No network identity for {NetId}");
                return;
            }

            switch(Action)
            {
                case ItemGroupPacketAction.Add:
                    AddStatusItemGroup(identity);
                    break;
                case ItemGroupPacketAction.Remove:
                    RemoveStatusItemGroup(identity);
                    break;
            }
        }

        public void AddStatusItemGroup(NetworkIdentity identity)
        {
            
        }

        public void RemoveStatusItemGroup(NetworkIdentity identity)
        {

        }
    }
}
