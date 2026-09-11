using HarmonyLib;
using ONI_Together.DebugTools;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.Components.Tools
{
    [SkipSaveFileSerialization]
    [FixedInterestGroup]
    public class AttackToolSyncer : NetworkBehaviour
    {
        public static AttackToolSyncer Instance { get; private set; }

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
                var go = new GameObject("AttackToolSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<AttackToolSyncer>();
            }
            Instance.NetId = nameof(AttackToolSyncer).GetHashCode();
        }

        public static void RequestAttack(Vector2 min, Vector2 max)
        {
            var s = Instance;
            if (s == null || ProcessingIncoming) return;
            try
            {
                var p = ToolMenu.Instance?.PriorityScreen?.GetLastSelectedPriority() ?? default;
                var originator = LocalUserIdQuery?.Invoke() ?? 0;
                if (MultiplayerSession.IsHost)
                    s.CallCommand(nameof(CmdAttack), min, max, (int)p.priority_class, p.priority_value, originator);
                else
                    s.CallCommand(nameof(CmdHostAttack), min, max, (int)p.priority_class, p.priority_value, originator);
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[AttackToolSyncer] {ex}");
            }
        }

        [Command]
        private void CmdAttack(Vector2 min, Vector2 max, int priority_class, int priority_value, ulong originator)
        {
            CallClientRpc(nameof(RpcAttack), min, max, priority_class, priority_value, originator);
        }

        [Command]
        private void CmdHostAttack(Vector2 min, Vector2 max, int priority_class, int priority_value, ulong originator)
        {
            ProcessingIncoming = true;
            try { ApplyAttack(min, max, priority_class, priority_value); }
            finally { ProcessingIncoming = false; }
            CallClientRpc(nameof(RpcAttack), min, max, priority_class, priority_value, originator);
        }

        [ClientRpc]
        private void RpcAttack(Vector2 min, Vector2 max, int priority_class, int priority_value, ulong originator)
        {
            if (originator != 0 && originator == (LocalUserIdQuery?.Invoke() ?? 0)) return;
            ProcessingIncoming = true;
            try { ApplyAttack(min, max, priority_class, priority_value); }
            finally { ProcessingIncoming = false; }
        }

        private static void ApplyAttack(Vector2 min, Vector2 max, int priority_class, int priority_value)
        {
            var ps = ToolMenu.Instance?.PriorityScreen;
            if (ps == null)
            {
                AttackTool.MarkForAttack(min, max, true);
                return;
            }
            var tr = Traverse.Create(ps).Field("lastSelectedPriority");
            var prev = tr.GetValue<PrioritySetting>();
            tr.SetValue(new PrioritySetting((PriorityScreen.PriorityClass)priority_class, priority_value));
            try { AttackTool.MarkForAttack(min, max, true); }
            finally { tr.SetValue(prev); }
        }
    }
}
