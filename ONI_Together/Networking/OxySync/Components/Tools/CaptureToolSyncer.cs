using HarmonyLib;
using ONI_Together.DebugTools;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.Components.Tools
{
    [SkipSaveFileSerialization]
    [FixedInterestGroup]
    public class CaptureToolSyncer : NetworkBehaviour
    {
        public static CaptureToolSyncer Instance { get; private set; }

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
                var go = new GameObject("CaptureToolSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<CaptureToolSyncer>();
            }
            Instance.NetId = nameof(CaptureToolSyncer).GetHashCode();
        }

        public static void RequestCapture(Vector2 min, Vector2 max)
        {
            var s = Instance;
            if (s == null || ProcessingIncoming) return;
            try
            {
                var p = ToolMenu.Instance?.PriorityScreen?.GetLastSelectedPriority() ?? default;
                var originator = LocalUserIdQuery?.Invoke() ?? 0;
                if (MultiplayerSession.IsHost)
                    s.CallCommand(nameof(CmdCapture), min, max, (int)p.priority_class, p.priority_value, originator);
                else
                    s.CallCommand(nameof(CmdHostCapture), min, max, (int)p.priority_class, p.priority_value, originator);
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[CaptureToolSyncer] {ex}");
            }
        }

        [Command]
        private void CmdCapture(Vector2 min, Vector2 max, int priority_class, int priority_value, ulong originator)
        {
            CallClientRpc(nameof(RpcCapture), min, max, priority_class, priority_value, originator);
        }

        [Command]
        private void CmdHostCapture(Vector2 min, Vector2 max, int priority_class, int priority_value, ulong originator)
        {
            ProcessingIncoming = true;
            try { ApplyCapture(min, max, priority_class, priority_value); }
            finally { ProcessingIncoming = false; }
            CallClientRpc(nameof(RpcCapture), min, max, priority_class, priority_value, originator);
        }

        [ClientRpc]
        private void RpcCapture(Vector2 min, Vector2 max, int priority_class, int priority_value, ulong originator)
        {
            if (originator != 0 && originator == (LocalUserIdQuery?.Invoke() ?? 0)) return;
            ProcessingIncoming = true;
            try { ApplyCapture(min, max, priority_class, priority_value); }
            finally { ProcessingIncoming = false; }
        }

        private static void ApplyCapture(Vector2 min, Vector2 max, int priority_class, int priority_value)
        {
            var ps = ToolMenu.Instance?.PriorityScreen;
            if (ps == null)
            {
                CaptureTool.MarkForCapture(min, max, true);
                return;
            }
            var tr = Traverse.Create(ps).Field("lastSelectedPriority");
            var prev = tr.GetValue<PrioritySetting>();
            tr.SetValue(new PrioritySetting((PriorityScreen.PriorityClass)priority_class, priority_value));
            try { CaptureTool.MarkForCapture(min, max, true); }
            finally { tr.SetValue(prev); }
        }
    }
}
