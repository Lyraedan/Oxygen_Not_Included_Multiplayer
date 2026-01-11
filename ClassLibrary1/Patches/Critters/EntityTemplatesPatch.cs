using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using UnityEngine;

namespace ONI_MP.Patches.Critters
{
    internal class EntityTemplatesPatch
    {
        [HarmonyPatch(typeof(EntityTemplates), nameof(EntityTemplates.ExtendEntityToBasicCreature), new Type[] { typeof(bool), typeof(GameObject), typeof(string), typeof(string), typeof(string), typeof(FactionManager.FactionID), typeof(string), typeof(string), typeof(NavType), typeof(int), typeof(float), typeof(string), typeof(float), typeof(bool), typeof(bool), typeof(float), typeof(float), typeof(float), typeof(float) })]
        public static class ExtendEntityToBasicCreature_Patch
        {
            public static void Postfix(GameObject __result)
            {
                if (__result == null)
                    return;

                var KPrefabID = __result.TryGetComponent<KPrefabID>(out var pid) ? pid.PrefabTag.ToString() : "NO KPrefabID";
                DebugConsole.Log($"[ExtendEntityToBasicCreature_Patch] Result prefab: {__result.name} | PrefabID: {KPrefabID}");

                if (!__result.HasTag(GameTags.Creature)) // I don't expect this to trigger ever
                    return;

                if (__result.GetComponent<EntityPositionHandler>() != null)
                    return;

                __result.AddOrGet<EntityPositionHandler>();

                DebugConsole.Log($"[ExtendEntityToBasicCreature_Patch] Added EntityPositionHandler to {__result.PrefabID().Name}");
            }
        }
    }
}
