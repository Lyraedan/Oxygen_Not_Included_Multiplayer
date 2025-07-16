using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Events;
using ONI_MP.Networking;
using UnityEngine;
using System.Security.Policy;
using ONI_MP.DebugTools;

namespace ONI_MP.Misc.World
{
    public static class EventTrigger
    {
        public static void TriggerEvent(GameObject go, int hash, object data)
        {
            if (!MultiplayerSession.IsHost)
            {
                DebugConsole.LogWarning("[EventTrigger] Client attempted to call TriggerEvent directly.");
                return;
            }

            KObject kObject = KObjectManager.Instance.Get(go);
            if (kObject != null && kObject.hasEventSystem)
            {
                kObject.GetEventSystem().Trigger(go, hash, data); // This is a different trigger function
                SendTriggerPacket(go, hash, data);
            }
        }

        public static void TriggerWithAuthority(GameObject go, int hash, object data)
        {
            KObject kObject = KObjectManager.Instance.Get(go);
            if (kObject != null && kObject.hasEventSystem)
            {
                kObject.GetEventSystem().Trigger(go, hash, data);
            }
        }

        private static void SendTriggerPacket(GameObject go, int hash, object data)
        {
            if (MultiplayerSession.IsHost)
            {
                NetworkIdentity identity = go.GetComponent<NetworkIdentity>();
                if (identity != null)
                {
                    var packet = new EventTriggeredPacket(identity.NetId, hash, data);
                    PacketSender.SendToAllClients(packet, SteamNetworkingSend.ReliableNoNagle);
                }
            }
        }
    }

}
