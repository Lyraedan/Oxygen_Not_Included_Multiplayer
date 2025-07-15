using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Events;
using UnityEngine;
using static STRINGS.UI.OUTFITS;

namespace ONI_MP.Patches.GamePatches
{
    [HarmonyPatch(typeof(EventExtensions))]
    public static class EventExtensionsPatch
    {
        // This is a lil bit of a jank way of doing it. TODO Improve this.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EventExtensions.Trigger))]
        public static bool Prefix(GameObject go, int hash, object data)
        {
            //DebugConsole.Log($"[MP] Trigger intercepted: {go.name} -> {hash}, Data: {data}");

            KObject kObject = KObjectManager.Instance.Get(go);
            if (kObject != null && kObject.hasEventSystem)
            {
                kObject.GetEventSystem().Trigger(go, hash, data);
                if(MultiplayerSession.IsHost)
                {
                    NetworkIdentity identity = go.GetComponent<NetworkIdentity>();
                    if(identity != null)
                    {
                        var packet = new EventTriggeredPacket(identity.NetId, hash, data);
                        PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
                    }
                }
            }
            return false;
        }
    }
}
