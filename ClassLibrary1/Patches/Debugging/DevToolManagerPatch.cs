using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Patches.Debugging
{
    using HarmonyLib;
    using ONI_MP.DebugTools;

    [HarmonyPatch(typeof(DevToolManager), MethodType.Constructor)]
    public static class DevToolManagerPatch
    {
        public static void Postfix(DevToolManager __instance)
        {
            __instance.menuNodes.AddAction("Debuggers/Multiplayer", delegate
            {
                __instance.panels.AddPanelFor<DevToolMultiplayer>();
            });

            __instance.devToolNameDict[typeof(DevToolMultiplayer)] = "Multiplayer";
        }
    }

}
