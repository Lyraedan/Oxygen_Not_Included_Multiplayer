using System.IO;
using HarmonyLib;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Profiling;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Attack;

public class AttackToolPacket : IPacket
{
    private ulong        SenderId = MultiplayerSession.LocalUserID;
    private Vector2         Min;
    private Vector2         Max;
    private PrioritySetting Priority = ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority();

    public AttackToolPacket()
    {
    }

    public AttackToolPacket(Vector2 min, Vector2 max)
    {
        using var _ = Profiler.Scope();

        Min = min;
        Max = max;
    }

    public void Serialize(BinaryWriter writer)
    {
        using var _ = Profiler.Scope();

        writer.Write(SenderId);
        writer.Write(Min);
        writer.Write(Max);
        writer.Write((int)Priority.priority_class);
        writer.Write(Priority.priority_value);
    }

    public void Deserialize(BinaryReader reader)
    {
        using var _ = Profiler.Scope();

        SenderId = reader.ReadUInt64();
        Min      = reader.ReadVector2();
        Max      = reader.ReadVector2();
        Priority = new PrioritySetting((PriorityScreen.PriorityClass)reader.ReadInt32(), reader.ReadInt32());
    }

    public void OnDispatched()
    {
        using var _ = Profiler.Scope();

        Traverse        lastSelectedPriority = Traverse.Create(ToolMenu.Instance.PriorityScreen).Field("lastSelectedPriority");
        PrioritySetting prioritySetting      = lastSelectedPriority.GetValue<PrioritySetting>();

        lastSelectedPriority.SetValue(Priority);

        AttackTool.MarkForAttack(Min, Max, true);

        lastSelectedPriority.SetValue(prioritySetting);
    }
}