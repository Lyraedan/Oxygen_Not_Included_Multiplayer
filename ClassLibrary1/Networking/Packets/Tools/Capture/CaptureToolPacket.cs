using System.IO;
using HarmonyLib;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Capture;

public class CaptureToolPacket : IPacket
{
    private CSteamID        SenderId = MultiplayerSession.LocalSteamID;
    private Vector2         Min;
    private Vector2         Max;
    private PrioritySetting Priority = ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority();

    public CaptureToolPacket()
    {
    }

    public CaptureToolPacket(Vector2 min, Vector2 max)
    {
        Min = min;
        Max = max;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(SenderId.m_SteamID);
        writer.Write(Min);
        writer.Write(Max);
        writer.Write((int)Priority.priority_class);
        writer.Write(Priority.priority_value);
    }

    public void Deserialize(BinaryReader reader)
    {
        SenderId = new CSteamID(reader.ReadUInt64());
        Min      = reader.ReadVector2();
        Max      = reader.ReadVector2();
        Priority = new PrioritySetting((PriorityScreen.PriorityClass)reader.ReadInt32(), reader.ReadInt32());
    }

    public void OnDispatched()
    {
        Traverse        lastSelectedPriority = Traverse.Create(ToolMenu.Instance.PriorityScreen).Field("lastSelectedPriority");
        PrioritySetting prioritySetting      = lastSelectedPriority.GetValue<PrioritySetting>();

        lastSelectedPriority.SetValue(Priority);

        CaptureTool.MarkForCapture(Min, Max, true);

        lastSelectedPriority.SetValue(prioritySetting);
    }
}