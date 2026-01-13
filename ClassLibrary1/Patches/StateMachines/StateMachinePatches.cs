using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace ONI_MP.Patches.StateMachines
{
    public class StateMachinePatches
    {
        [HarmonyPatch(typeof(StateMachine.Instance), nameof(StateMachine.Instance.IsRunning))]
        public static class StateMachine_IsRunning_Patch
        {
            static bool Prefix(StateMachine.Instance __instance, ref bool __result)
            {
                if (__instance.IsSMIPaused())
                {
                    __result = true;
                    return false;
                }

                return true; // run original
            }
        }
    }
}
