using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.Components;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

namespace ONI_Together.Patches.World
{
    public static class ClearablePatches
    {
        [HarmonyPatch(typeof(Clearable), nameof(Clearable.MarkForClear))]
        public static class ClearableMarkForClearPatch
        {
            public static void Postfix(Clearable __instance, bool restoringFromSave, bool allowWhenStored)
            {
                using var _ = Profiler.Scope();
                if (restoringFromSave) return;
                if (!MultiplayerSession.InActiveSession) return;
                if (__instance == null || __instance.gameObject == null) return;
                var identity = __instance.gameObject.GetNetIdentity();
                int netId = identity != null ? identity.NetId : 0;
                int cell = Grid.PosToCell(__instance.gameObject);
                BuildingActionSyncer.RequestClearable(netId, cell, true);
            }
        }

        [HarmonyPatch(typeof(Clearable), nameof(Clearable.CancelClearing))]
        public static class ClearableCancelClearingPatch
        {
            public static void Postfix(Clearable __instance)
            {
                using var _ = Profiler.Scope();
                if (!MultiplayerSession.InActiveSession) return;
                if (__instance == null || __instance.gameObject == null) return;
                var identity = __instance.gameObject.GetNetIdentity();
                int netId = identity != null ? identity.NetId : 0;
                int cell = Grid.PosToCell(__instance.gameObject);
                BuildingActionSyncer.RequestClearable(netId, cell, false);
            }
        }
    }
}
