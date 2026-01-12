using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Social
{
    public class ScheduleRowPacket : IPacket
    {
        // Use for shifting groups
        public enum RowAction
        {
            SHIFT_UP,
            SHIFT_DOWN,
            ROTATE_LEFT,
            ROTATE_RIGHT,
            DUPLICATE,
            DELETE
        }

        public int ScheduleIndex;
        public RowAction Action;

        // Shifting / Rotating
        public int TimetableToIndex;

        // Duplicating
        public List<ScheduleBlock> NewBlocks = new List<ScheduleBlock>();

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ScheduleIndex);
            writer.Write((int)Action);
            writer.Write(TimetableToIndex);
            switch(Action)
            {
                case RowAction.DUPLICATE:
                    writer.Write(NewBlocks.Count);
                    foreach(ScheduleBlock block in NewBlocks)
                    {
                        writer.Write(block.name);
                        writer.Write(block.GroupId);
                    }
                    break;
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            ScheduleIndex = reader.ReadInt32();
            Action = (RowAction)reader.ReadInt32();
            TimetableToIndex = reader.ReadInt32();
            switch(Action)
            {
                case RowAction.DUPLICATE:
                    NewBlocks.Clear();
                    int block_count = reader.ReadInt32();
                    for(int i = 0; i < block_count; i++)
                    {
                        string block_name = reader.ReadString();
                        string group_id = reader.ReadString();
                        ScheduleBlock block = new ScheduleBlock(block_name, group_id);
                        NewBlocks.Add(block);
                    }
                    break;
            }
        }

        public void OnDispatched()
        {
            if (IsApplying)
                return;

            try
            {
                IsApplying = true;
                Apply();
            } finally
            {
                IsApplying = false;
            }
        }

        public void Apply()
        {
            var schedules = ScheduleManager.Instance.schedules;
            if (schedules == null)
                return;

            Schedule schedule = schedules[ScheduleIndex];

            switch(Action)
            {
                case RowAction.SHIFT_UP:
                    ShiftUp(schedule);
                    break;
                case RowAction.SHIFT_DOWN:
                    ShiftDown(schedule);
                    break;
                case RowAction.ROTATE_LEFT:
                    RotateLeft(schedule);
                    break;
                case RowAction.ROTATE_RIGHT:
                    RotateRight(schedule);
                    break;
                case RowAction.DUPLICATE:
                    DuplicateRow(schedule);
                    break;
                case RowAction.DELETE:
                    DeleteRow(schedule);
                    break;
                default:
                    break;
            }
        }

        public void ShiftUp(Schedule schedule)
        {
            schedule.ShiftTimetable(true, TimetableToIndex);
        }

        public void ShiftDown(Schedule schedule)
        {
            schedule.ShiftTimetable(false, TimetableToIndex);
        }

        public void RotateLeft(Schedule schedule)
        {
            schedule.RotateBlocks(true, TimetableToIndex);
        }

        public void RotateRight(Schedule schedule)
        {
            schedule.RotateBlocks(false, TimetableToIndex);
        }

        public void DuplicateRow(Schedule schedule)
        {
            schedule.InsertTimetable(TimetableToIndex, NewBlocks);
        }

        public void DeleteRow(Schedule schedule)
        {
            // plz do not explode
            if (ScheduleScreen.Instance.scheduleEntries.Count <= ScheduleIndex)
                return;

            ScheduleScreenEntry entry = ScheduleScreen.Instance.scheduleEntries[ScheduleIndex];
            if(entry != null)
            {
                if (entry.timetableRows.Count <= TimetableToIndex)
                    return; // plz do not also explode

                GameObject row = entry.timetableRows[TimetableToIndex];
                if (row != null)
                {
                    entry.RemoveTimetableRow(row);
                }
            } else
            {
                schedule.RemoveTimetable(TimetableToIndex);
            }
        }

        public static bool IsApplying = false;

    }
}
