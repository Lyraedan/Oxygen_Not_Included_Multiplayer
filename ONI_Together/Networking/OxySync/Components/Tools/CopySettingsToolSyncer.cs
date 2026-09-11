using ONI_Together.DebugTools;
using ONI_Together.Networking.Components;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.Components.Tools
{
    [SkipSaveFileSerialization]
    [FixedInterestGroup]
    public class CopySettingsToolSyncer : NetworkBehaviour
    {
        public static CopySettingsToolSyncer Instance { get; private set; }

        public static bool ProcessingIncoming;

        public override void OnSpawn()
        {
            base.OnSpawn();
            Instance = this;
            InterestGroup = -1;
        }

        public override void OnCleanUp()
        {
            if (Instance == this)
                Instance = null;
            base.OnCleanUp();
        }

        public static void RegisterNetId(GameObject parent = null)
        {
            var root = parent == null ? Game.Instance.gameObject : parent;
            if (Instance == null)
            {
                var go = new GameObject("CopySettingsToolSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<CopySettingsToolSyncer>();
            }
            Instance.NetId = nameof(CopySettingsToolSyncer).GetHashCode();
        }

        public static void RequestCopy(int netId, int cell)
        {
            var s = Instance;
            if (s == null || ProcessingIncoming) return;
            try
            {
                var originator = LocalUserIdQuery?.Invoke() ?? 0;
                if (MultiplayerSession.IsHost)
                    s.CallCommand(nameof(CmdCopy), netId, cell, originator);
                else
                    s.CallCommand(nameof(CmdHostCopy), netId, cell, originator);
            }
            catch (System.Exception ex) { DebugConsole.LogWarning($"[CopySettingsToolSyncer] {ex}"); }
        }

        [Command]
        private void CmdCopy(int netId, int cell, ulong originator)
        {
            CallClientRpc(nameof(RpcCopy), netId, cell, originator);
        }

        [Command]
        private void CmdHostCopy(int netId, int cell, ulong originator)
        {
            ProcessingIncoming = true;
            try { ApplyCopy(netId, cell); }
            finally { ProcessingIncoming = false; }
            CallClientRpc(nameof(RpcCopy), netId, cell, originator);
        }

        [ClientRpc]
        private void RpcCopy(int netId, int cell, ulong originator)
        {
            if (originator != 0 && originator == (LocalUserIdQuery?.Invoke() ?? 0)) return;
            ProcessingIncoming = true;
            try { ApplyCopy(netId, cell); }
            finally { ProcessingIncoming = false; }
        }

        private static void ApplyCopy(int netId, int cell)
        {
            if (!NetworkIdentityRegistry.TryGet(netId, out var identity) || identity.gameObject == null || !identity.TryGetComponent<CopyBuildingSettings>(out var sourceSettings) || !identity.TryGetComponent<KPrefabID>(out var sourceId))
                return;
            var targetId = CopyBuildingSettings.ResolveTarget(CopyBuildingSettings.ResolveLayer(identity.gameObject), cell);
            if (targetId == null) return;
            CopyBuildingSettings.ApplyCopy(targetId, identity.gameObject, sourceId, sourceSettings);
            Game.Instance.userMenu.Refresh(targetId.gameObject);
        }
    }
}
