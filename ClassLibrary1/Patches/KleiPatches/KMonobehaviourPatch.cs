using HarmonyLib;
using ONI_MP.Misc.World;
using ONI_MP.Networking;
using UnityEngine;

namespace ONI_MP.Patches.KleiPatches
{
    [HarmonyPatch(typeof(KMonoBehaviour))]
    public static class KMonobehaviourPatch
    {
        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(KMonoBehaviour.Trigger), new[] { typeof(int), typeof(object) })]
        //public static bool Prefix(KMonoBehaviour __instance, int hash, object data)
        //{
        //    if(!MultiplayerSession.InSession)
        //    {
        //        return true;
        //    }


        //    GameObject go = __instance.gameObject;

        //    // Only the host is allowed to dispatch events directly
        //    if (MultiplayerSession.IsHost)
        //    {
        //        EventTrigger.TriggerEvent(go, hash, data);
        //    }

        //    return true;
        //}
    }
}
