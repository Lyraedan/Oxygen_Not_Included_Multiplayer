using ONI_MP.Networking.Packets.Architecture;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Dig
{
    public class DiggablePacket : IPacket
    {
        /// <summary>
        /// Gets a value indicating whether incoming messages are currently being processed.
        /// Use in patches to prevent recursion when applying tool changes.
        /// </summary>
        public static bool ProcessingIncoming { get; private set; }

        private int             Cell;
        private int             AnimationDelay;
        private PrioritySetting Priority = ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority();

        public DiggablePacket()
        {
        }

        public DiggablePacket(int cell, int animationDelay)
        {
            Cell           = cell;
            AnimationDelay = animationDelay;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Cell);
            writer.Write(AnimationDelay);
            writer.Write((int)Priority.priority_class);
            writer.Write(Priority.priority_value);
        }

        public void Deserialize(BinaryReader reader)
        {
            Cell           = reader.ReadInt32();
            AnimationDelay = reader.ReadInt32();
            Priority       = new PrioritySetting((PriorityScreen.PriorityClass)reader.ReadInt32(), reader.ReadInt32());
        }

        public void OnDispatched()
        {
            ProcessingIncoming = true;
            GameObject game_object = DigTool.PlaceDig(Cell, AnimationDelay);
            ProcessingIncoming = false;

            Prioritizable prioritizable = game_object?.GetComponent<Prioritizable>();
            prioritizable?.SetMasterPriority(Priority);
        }
    }
}