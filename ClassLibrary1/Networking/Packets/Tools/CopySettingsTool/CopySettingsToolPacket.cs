using System.IO;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;

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
        NetID = netID;
        Cell  = cell;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NetID);
        writer.Write(Cell);
    }

    public void Deserialize(BinaryReader reader)
    {
        NetID = reader.ReadInt32();
        Cell  = reader.ReadInt32();
    }

    public void OnDispatched()
    {
        NetworkIdentity identity;
        if (!NetworkIdentityRegistry.TryGet(NetID, out identity) 
            || identity.gameObject == null 
            || !identity.TryGetComponent<CopyBuildingSettings>(out var sourceSettings)
            || !identity.TryGetComponent<KPrefabID>(out var sourceId))
            return;

		KPrefabID targetId = CopyBuildingSettings.ResolveTarget(CopyBuildingSettings.ResolveLayer(identity.gameObject), Cell);
        if (targetId == null)
            return;

		CopyBuildingSettings.ApplyCopy(targetId, identity.gameObject, sourceId, sourceSettings);
        Game.Instance.userMenu.Refresh(targetId.gameObject);
    }
}