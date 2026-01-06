using System.IO;
using HarmonyLib;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.Tools.Harvest;

public class HarvestToolPacket : IPacket
{
    /// <summary>
    /// Gets a value indicating whether incoming messages are currently being processed.
    /// Use in patches to prevent recursion when applying tool changes.
    /// </summary>
    public static bool ProcessingIncoming { get; private set; }

    private int             Cell;
    private PrioritySetting Priority = ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority();

    public HarvestToolPacket()
    {
    }

    public HarvestToolPacket(int cell)
    {
        Cell = cell;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Cell);
        writer.Write((int)Priority.priority_class);
        writer.Write(Priority.priority_value);
    }

    public void Deserialize(BinaryReader reader)
    {
        Cell     = reader.ReadInt32();
        Priority = new PrioritySetting((PriorityScreen.PriorityClass)reader.ReadInt32(), reader.ReadInt32());
    }

    public void OnDispatched()
    {
        Traverse        lastSelectedPriority = Traverse.Create(ToolMenu.Instance.PriorityScreen).Field("lastSelectedPriority");
        PrioritySetting prioritySetting      = lastSelectedPriority.GetValue<PrioritySetting>();

        lastSelectedPriority.SetValue(Priority);

        ProcessingIncoming = true;
        HarvestTool.Instance.OnDragTool(Cell, 0);
        ProcessingIncoming = false;

        lastSelectedPriority.SetValue(prioritySetting);
    }
}