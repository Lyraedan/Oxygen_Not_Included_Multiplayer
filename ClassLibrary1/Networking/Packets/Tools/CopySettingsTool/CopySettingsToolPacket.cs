using System.IO;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Profiling;

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
        Profiler.Active.Scope();

        NetID = netID;
        Cell  = cell;
    }

    public void Serialize(BinaryWriter writer)
    {
        Profiler.Active.Scope();

        writer.Write(NetID);
        writer.Write(Cell);
    }

    public void Deserialize(BinaryReader reader)
    {
        Profiler.Active.Scope();

        NetID = reader.ReadInt32();
        Cell  = reader.ReadInt32();
    }

    public void OnDispatched()
    {
        Profiler.Active.Scope();

        NetworkIdentity identity;
        if (!NetworkIdentityRegistry.TryGet(NetID, out identity))
            return;

        CopyBuildingSettings.ApplyCopy(Cell, identity.gameObject);
        Game.Instance.userMenu.Refresh(identity.gameObject);
    }
}