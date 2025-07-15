using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.Networking.Components;
using UnityEngine;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch(typeof(Substance), nameof(Substance.SpawnResource))]
    public static class Substance_SpawnResource_Patch
    {
        public static void Postfix(GameObject __result)
        {
            if (__result == null)
                return;

            NetworkIdentity identity = __result.AddOrGet<NetworkIdentity>();
            identity.RegisterIdentity();
        }
    }

}
