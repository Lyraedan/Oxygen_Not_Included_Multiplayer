using HarmonyLib;
using UnityEngine;
using Klei.AI;
using ONI_MP.DebugTools;

namespace ONIMod.Logging
{
    [HarmonyPatch]
    public class ConditionChangePatch
    {
        // Log health changes
        [HarmonyPatch(typeof(Health), nameof(Health.OnHealthChanged))]
        [HarmonyPostfix]
        public static void HealthChanged_Postfix(Health __instance, float delta)
        {
            var minion = __instance.GetComponent<MinionIdentity>();
            if (minion != null)
            {
                //DebugConsole.Log($"[Logger] {minion.name} - Health changed by {delta}, now: {__instance.hitPoints}/{__instance.maxHitPoints}");
            }
        }

        // Log when an attribute modifier is added
        [HarmonyPatch(typeof(AttributeInstance), nameof(AttributeInstance.Add))]
        [HarmonyPostfix]
        public static void AttributeAdd_Postfix(AttributeInstance __instance, AttributeModifier modifier)
        {
            var minion = __instance.gameObject.GetComponent<MinionIdentity>();
            if (minion != null)
            {
                //DebugConsole.Log($"[Logger] {minion.name} - Attribute '{__instance.Id}' added modifier {modifier.Value} ({modifier.Description})");
            }
        }

        // Log when an attribute modifier is removed
        [HarmonyPatch(typeof(AttributeInstance), nameof(AttributeInstance.Remove))]
        [HarmonyPostfix]
        public static void AttributeRemove_Postfix(AttributeInstance __instance, AttributeModifier modifier)
        {
            var minion = __instance.gameObject.GetComponent<MinionIdentity>();
            if (minion != null)
            {
                //DebugConsole.Log($"[Logger] {minion.name} - Attribute '{__instance.Id}' removed modifier {modifier.Value} ({modifier.Description})");
            }
        }

        // Log when an amount value is set (e.g., Stress, Breath, Calories)
        [HarmonyPatch(typeof(AmountInstance), nameof(AmountInstance.SetValue))]
        [HarmonyPostfix]
        public static void AmountSet_Postfix(AmountInstance __instance, float value)
        {
            var minion = __instance.gameObject.GetComponent<MinionIdentity>();
            if (minion != null)
            {
                string id = __instance.amount?.Id ?? "Unknown";
                //DebugConsole.Log($"[Logger] {minion.name} - Amount '{id}' set to {value}");
            }
        }
    }
}
