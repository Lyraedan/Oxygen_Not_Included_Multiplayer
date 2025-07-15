using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using System.Reflection;

namespace ONI_MP.Patches
{
    [HarmonyPatch]
    public static class DoLoadPatch
    {
        // Explicitly resolve the exact DoLoad(string) method
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return typeof(LoadScreen).GetMethod(
                "DoLoad",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null
            );
        }

        // Updating this bool here doesn't affect SP
        [HarmonyPrefix]
        public static void Prefix_DoLoad(string filename)
        {
            DebugConsole.Log($"Loading {filename}");
        }

        [HarmonyPostfix]
        public static void Postfix_DoLoad(string filename)
        {
            DebugConsole.Log($"Loaded {filename}");
        }
    }
}
