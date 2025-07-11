using HarmonyLib;
using ONI_MP.Misc.World;
using ONI_MP.Networking;
using UnityEngine;

[HarmonyPatch(typeof(EventExtensions))]
public static class EventExtensionsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(EventExtensions.Trigger))]
    public static bool Prefix(GameObject go, int hash, object data)
    {
        // Only the host should forward and trigger events.
        if (MultiplayerSession.IsHost)
        {
            EventTrigger.TriggerEvent(go, hash, data);
        }

        // Always block original trigger (handled manually)
        return false;
    }
}
