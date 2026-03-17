using ONI_MP.Networking.Packets.Architecture;
using System.IO;
using ONI_MP.Networking.Components;
using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
    public class ConsumablePermissionPacket : IPacket
    {
        private TableRow.RowType         RowType;
        private string                   ConsumableID;
        private TableScreen.ResultValues NewValue;
        private int                      NetId;

        public ConsumablePermissionPacket()
        {
        }

        public ConsumablePermissionPacket(TableRow.RowType rowType, string consumableID, TableScreen.ResultValues newValue, int netId)
        {
            Profiler.Active.Scope();

            RowType      = rowType;
            ConsumableID = consumableID;
            NewValue     = newValue;
            NetId        = netId;
        }

        public void Serialize(BinaryWriter writer)
        {
            Profiler.Active.Scope();

            writer.Write((int)RowType);
            writer.Write(ConsumableID);
            writer.Write((int)NewValue);

            if (RowType == TableRow.RowType.Minion)
                writer.Write(NetId);
        }

        public void Deserialize(BinaryReader reader)
        {
            Profiler.Active.Scope();

            RowType      = (TableRow.RowType)reader.ReadInt32();
            ConsumableID = reader.ReadString();
            NewValue     = (TableScreen.ResultValues)reader.ReadInt32();

            if (RowType == TableRow.RowType.Minion)
                NetId = reader.ReadInt32();
        }

        public void OnDispatched()
        {
            Profiler.Active.Scope();

            switch (RowType)
            {
                case TableRow.RowType.Default:
                {
                    if (NewValue == TableScreen.ResultValues.True)
                        ConsumerManager.instance.DefaultForbiddenTagsList.Remove(ConsumableID.ToTag());
                    else
                        ConsumerManager.instance.DefaultForbiddenTagsList.Add(ConsumableID.ToTag());

                    break;
                }
                case TableRow.RowType.Minion:
                {
                    NetworkIdentity identity;
                    if (!NetworkIdentityRegistry.TryGet(NetId, out identity))
                        return;

                    ConsumableConsumer component   = identity.GetComponent<ConsumableConsumer>();
                    bool               can_consume = NewValue is TableScreen.ResultValues.True or TableScreen.ResultValues.ConditionalGroup;

                    component?.SetPermitted(ConsumableID, can_consume);
                    break;
                }
            }

            ManagementMenu.Instance.consumablesScreen.MarkRowsDirty();
        }
    }
}