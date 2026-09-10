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
        private static bool _processing;

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
            if (s == null) return;
            try
            {
                var p = ToolMenu.Instance?.PriorityScreen?.GetLastSelectedPriority() ?? default;
                s.CallCommand(nameof(CmdAttack), min, max, (int)p.priority_class, p.priority_value);
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[AttackToolSyncer] {ex}");
            }
        }

        [Command]
        private void CmdAttack(Vector2 min, Vector2 max, int priority_class, int priority_value)
        {
            CallClientRpc(nameof(RpcAttack), min, max, priority_class, priority_value);
        }

        [ClientRpc]
        private void RpcAttack(Vector2 min, Vector2 max, int priority_class, int priority_value)
        {
            if (_processing) return;
            _processing = true;
            try
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
            finally { _processing = false; }
        }
    }
}
