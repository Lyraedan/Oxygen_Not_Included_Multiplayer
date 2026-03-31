using System.IO;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Profiling;

namespace ONI_MP.Networking.Packets.Tools.CopySettingsTool;

public class CopySettingsToolPacket : IPacket
{
    private int NetID;
    private int Cell;

    public CopySettingsToolPacket()
    {
    }

    public CopySettingsToolPacket(int netID, int cell)
    {
        using var _ = Profiler.Scope();

        NetID = netID;
        Cell  = cell;
    }

    public void Serialize(BinaryWriter writer)
    {
        using var _ = Profiler.Scope();

        writer.Write(NetID);
        writer.Write(Cell);
    }

    public void Deserialize(BinaryReader reader)
    {
        using var _ = Profiler.Scope();

        NetID = reader.ReadInt32();
        Cell  = reader.ReadInt32();
    }

    public void OnDispatched()
    {
        using var _ = Profiler.Scope();

        NetworkIdentity identity;
        if (!NetworkIdentityRegistry.TryGet(NetID, out identity))
            return;

        var targetGO = Grid.Objects[Cell, (int)ObjectLayer.Building];
        if (targetGO == null)
            return;
        var targetId = targetGO.GetComponent<KPrefabID>();
        var sourceId = identity.gameObject.GetComponent<KPrefabID>();
        var sourceSettings = identity.gameObject.GetComponent<CopyBuildingSettings>();
        if (targetId == null || sourceId == null || sourceSettings == null)
            return;
        CopyBuildingSettings.ApplyCopy(targetId, identity.gameObject, sourceId, sourceSettings);
        Game.Instance.userMenu.Refresh(targetGO);
    }
}