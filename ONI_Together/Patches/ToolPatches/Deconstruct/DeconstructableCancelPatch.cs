using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.Components;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

namespace ONI_Together.Patches.ToolPatches.Deconstruct
{
    [HarmonyPatch(typeof(Deconstructable), nameof(Deconstructable.CancelDeconstruction))]
    public static class DeconstructableCancelPatch
    {
        public static void Postfix(Deconstructable __instance)
        {
            using var _ = Profiler.Scope();
            if (!MultiplayerSession.InActiveSession) return;
            var identity = __instance.GetComponent<NetworkIdentity>();
            if (identity == null || identity.NetId == 0) return;
            BuildingActionSyncer.RequestAction(identity.NetId, 2);
        }
    }
}
