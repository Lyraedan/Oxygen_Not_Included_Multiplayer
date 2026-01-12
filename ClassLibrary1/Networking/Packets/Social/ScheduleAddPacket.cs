using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.Social
{
    public class ScheduleAddPacket : IPacket
    {
        public string Name;
        public List<ScheduleBlock> Blocks = new List<ScheduleBlock>();
        public bool AlarmActivated;
        public bool Duplicated;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Blocks.Count);
            foreach (ScheduleBlock block in Blocks)
            {
                writer.Write(block.name);
                writer.Write(block.GroupId);
            }
            writer.Write(AlarmActivated);
            writer.Write(Duplicated);
        }

        public void Deserialize(BinaryReader reader)
        {
            Name = reader.ReadString();
            Blocks.Clear();
            int blocks_count = reader.ReadInt32();
            for(int i = 0; i < blocks_count; i++)
            {
                string blockName = reader.ReadString();
                string groupId = reader.ReadString();
                ScheduleBlock block = new ScheduleBlock(blockName, groupId);
                Blocks.Add(block);
            }
            AlarmActivated = reader.ReadBoolean();
            Duplicated = reader.ReadBoolean();
        }

        public void OnDispatched()
        {
            if (IsApplying)
                return;

            if (MultiplayerSession.IsHost)
            {
                Apply();
                PacketSender.SendToAllClients(this);
            } else
            {
                Apply();
            }
        }

        private void Apply()
        {
            try
            {
                IsApplying = true;
                if (Duplicated)
                {
                    DuplicateSchedule();
                }
                else
                {
                    AddNewSchedule();
                }
            } finally
            {
                IsApplying = false;
            }
        }

        private void AddNewSchedule()
        {
            ScheduleManager.Instance.AddSchedule(Db.Get().ScheduleGroups.allGroups, Name, AlarmActivated);
        }

        private void DuplicateSchedule()
        {
            Schedule source = new Schedule(Name, Blocks, AlarmActivated);
            ScheduleManager.Instance.DuplicateSchedule(source);
        }

        public static bool IsApplying = false;
    }
}
