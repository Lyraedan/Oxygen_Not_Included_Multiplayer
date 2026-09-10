using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.Components;
using ONI_Together.Networking.OxySync.Components.Tools;
using Shared.Profiling;

namespace ONI_Together.Patches.ToolPatches.Cancel
{
    [HarmonyPatch(typeof(Constructable), "OnCancel")]
    public static class ConstructableCancelPatch
    {
        public static void Postfix(Constructable __instance)
        {
            using var _ = Profiler.Scope();
            if (!MultiplayerSession.InActiveSession) return;
            var identity = __instance.GetComponent<NetworkIdentity>();
            if (identity == null || identity.NetId == 0) return;
            BuildingActionSyncer.RequestAction(identity.NetId, 3);
        }
    }
}
