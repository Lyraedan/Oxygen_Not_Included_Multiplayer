using ONI_Together.DebugTools;
using ONI_Together.Networking.Components;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.Components.Tools
{
    [SkipSaveFileSerialization]
    [FixedInterestGroup]
    public class BuildingActionSyncer : NetworkBehaviour
    {
        public static BuildingActionSyncer Instance { get; private set; }

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
                var go = new GameObject("BuildingActionSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<BuildingActionSyncer>();
            }
            Instance.NetId = nameof(BuildingActionSyncer).GetHashCode();
        }

        public static void RequestAction(int netId, byte action)
        {
            var s = Instance;
            if (s == null || ProcessingIncoming) return;
            try
            {
                var originator = LocalUserIdQuery?.Invoke() ?? 0;
                if (MultiplayerSession.IsHost)
                    s.CallCommand(nameof(CmdAction), netId, action, originator);
                else
                    s.CallCommand(nameof(CmdHostAction), netId, action, originator);
            }
            catch (System.Exception ex) { DebugConsole.LogWarning($"[BuildingActionSyncer] {ex}"); }
        }

        public static void RequestClearable(int netId, int cell, bool isMarked)
        {
            var s = Instance;
            if (s == null || ProcessingIncoming) return;
            try
            {
                var originator = LocalUserIdQuery?.Invoke() ?? 0;
                if (MultiplayerSession.IsHost)
                    s.CallCommand(nameof(CmdClearable), netId, cell, isMarked, originator);
                else
                    s.CallCommand(nameof(CmdHostClearable), netId, cell, isMarked, originator);
            }
            catch (System.Exception ex) { DebugConsole.LogWarning($"[BuildingActionSyncer] {ex}"); }
        }

        public static void RequestDeconstructComplete(int cell, int objectLayer)
        {
            var s = Instance;
            if (s == null || ProcessingIncoming) return;
            try
            {
                var originator = LocalUserIdQuery?.Invoke() ?? 0;
                if (MultiplayerSession.IsHost)
                    s.CallCommand(nameof(CmdDeconstructComplete), cell, objectLayer, originator);
                else
                    s.CallCommand(nameof(CmdHostDeconstructComplete), cell, objectLayer, originator);
            }
            catch (System.Exception ex) { DebugConsole.LogWarning($"[BuildingActionSyncer] {ex}"); }
        }

        [Command]
        private void CmdAction(int netId, byte action, ulong originator)
        {
            CallClientRpc(nameof(RpcAction), netId, action, originator);
        }

        [Command]
        private void CmdHostAction(int netId, byte action, ulong originator)
        {
            ProcessingIncoming = true;
            try { ApplyAction(netId, action); }
            finally { ProcessingIncoming = false; }
            CallClientRpc(nameof(RpcAction), netId, action, originator);
        }

        [ClientRpc]
        private void RpcAction(int netId, byte action, ulong originator)
        {
            if (originator != 0 && originator == (LocalUserIdQuery?.Invoke() ?? 0)) return;
            ProcessingIncoming = true;
            try { ApplyAction(netId, action); }
            finally { ProcessingIncoming = false; }
        }

        private static void ApplyAction(int netId, byte action)
        {
            if (!NetworkIdentityRegistry.TryGet(netId, out var identity) || identity == null || identity.gameObject == null) return;
            var go = identity.gameObject;
            switch ((byte)action)
            {
                case 1:
                    if (go.TryGetComponent<Deconstructable>(out var dq)) dq.QueueDeconstruction(true);
                    break;
                case 2:
                    if (go.TryGetComponent<Deconstructable>(out var dc)) dc.CancelDeconstruction();
                    break;
                case 3:
                    go.Trigger(2127324410);
                    break;
            }
        }

        [Command]
        private void CmdClearable(int netId, int cell, bool isMarked, ulong originator)
        {
            CallClientRpc(nameof(RpcClearable), netId, cell, isMarked, originator);
        }

        [Command]
        private void CmdHostClearable(int netId, int cell, bool isMarked, ulong originator)
        {
            ProcessingIncoming = true;
            try { ApplyClearable(netId, cell, isMarked); }
            finally { ProcessingIncoming = false; }
            CallClientRpc(nameof(RpcClearable), netId, cell, isMarked, originator);
        }

        [ClientRpc]
        private void RpcClearable(int netId, int cell, bool isMarked, ulong originator)
        {
            if (originator != 0 && originator == (LocalUserIdQuery?.Invoke() ?? 0)) return;
            ProcessingIncoming = true;
            try { ApplyClearable(netId, cell, isMarked); }
            finally { ProcessingIncoming = false; }
        }

        private static void ApplyClearable(int netId, int cell, bool isMarked)
        {
            Clearable target = null;
            if (netId != 0 && NetworkIdentityRegistry.TryGet(netId, out var identity) && identity != null)
                target = identity.GetComponent<Clearable>();
            if (target == null && Grid.IsValidCell(cell))
            {
                var go = Grid.Objects[cell, (int)ObjectLayer.Pickupables];
                if (go != null) target = go.GetComponent<Clearable>();
            }
            if (target == null) return;
            if (isMarked) target.MarkForClear();
            else target.CancelClearing();
        }

        [Command]
        private void CmdDeconstructComplete(int cell, int objectLayer, ulong originator)
        {
            CallClientRpc(nameof(RpcDeconstructComplete), cell, objectLayer, originator);
        }

        [Command]
        private void CmdHostDeconstructComplete(int cell, int objectLayer, ulong originator)
        {
            ProcessingIncoming = true;
            try { ApplyDeconstructComplete(cell, objectLayer); }
            finally { ProcessingIncoming = false; }
            CallClientRpc(nameof(RpcDeconstructComplete), cell, objectLayer, originator);
        }

        [ClientRpc]
        private void RpcDeconstructComplete(int cell, int objectLayer, ulong originator)
        {
            if (originator != 0 && originator == (LocalUserIdQuery?.Invoke() ?? 0)) return;
            ProcessingIncoming = true;
            try { ApplyDeconstructComplete(cell, objectLayer); }
            finally { ProcessingIncoming = false; }
        }

        private static void ApplyDeconstructComplete(int cell, int objectLayer)
        {
            if (!Grid.IsValidCell(cell)) return;
            var go = Grid.Objects[cell, objectLayer];
            if (go == null) return;
            if (go.TryGetComponent<Deconstructable>(out var d) && !d.HasBeenDestroyed) Util.KDestroyGameObject(go);
        }
    }
}
