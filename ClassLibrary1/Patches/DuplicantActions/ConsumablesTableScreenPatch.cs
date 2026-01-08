using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.DuplicantActions;
using UnityEngine;

namespace ONI_MP.Patches.DuplicantActions
{
    [HarmonyPatch(typeof(ConsumablesTableScreen), nameof(ConsumablesTableScreen.set_value_consumable_info))]
    public static class ConsumablesTableScreenPatch
    {
        public static void Postfix(GameObject widget_go, TableScreen.ResultValues new_value, ConsumablesTableScreen __instance)
        {
            if (!MultiplayerSession.InSession)
                return;


            TableRow                  widgetRow    = __instance.GetWidgetRow(widget_go);
            ConsumableInfoTableColumn widgetColumn = __instance.GetWidgetColumn(widget_go) as ConsumableInfoTableColumn;
            if (widgetRow == null || widgetColumn == null)
                return;

            IConsumableUIItem consumableInfo = widgetColumn.consumable_info;
            MinionIdentity    minionIdentity = widgetRow.GetIdentity() as MinionIdentity;
            NetworkIdentity   identity       = minionIdentity?.GetComponent<NetworkIdentity>();

            int id = -1;
            if (identity != null)
                id = identity.NetId;

            PacketSender.SendToAllOtherPeers(new ConsumablePermissionPacket(widgetRow.rowType, consumableInfo.ConsumableId, new_value, id));
        }
    }
}