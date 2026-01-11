using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Patches.World.SideScreen;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch(typeof(Toggleable), nameof(Toggleable.Toggle))]
    public static class QueueToggleable
    {
        static void Postfix(Toggleable __instance, int targetIdx)
        {
            if (!MultiplayerSession.InSession) return;

            bool expectedQueue = __instance.IsToggleQueued(targetIdx);
            SideScreenSyncHelper.SyncQueueToggleable(__instance.gameObject, expectedQueue);
        }
    }

    [HarmonyPatch(typeof(Toggleable), "OnCompleteWork")]
    public static class ToggleableCompleteWorkPatch
    {
        static void Prefix(Toggleable __instance, out IToggleHandler __state, WorkerBase worker)
        {
            // Get the toggle handler for the completed work. 
            int targetForWorker = __instance.GetTargetForWorker(worker);
            __state = targetForWorker != -1 ? __instance.targets[targetForWorker].Key : null;

            return;
        }

        static void Postfix(Toggleable __instance, IToggleHandler __state)
        {
            if (!MultiplayerSession.InSession) return;

            if (__state != null)
            {
                bool isOn = __state.IsHandlerOn();
                DebugConsole.Log($"[ToggleablePatch] Toggleable for {__instance.gameObject.name} Changed");
                SideScreenSyncHelper.SyncToggleableState(__instance.gameObject, isOn);
            }
        }
    }
}
