using ONI_Together.DebugTools;
using ONI_Together.Networking.Components;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.Components.Tools
{
    [SkipSaveFileSerialization]
    [FixedInterestGroup]
    public class DigToolSyncer : NetworkBehaviour
    {
        public static DigToolSyncer Instance { get; private set; }

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
                var go = new GameObject("DigToolSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<DigToolSyncer>();
            }
            Instance.NetId = nameof(DigToolSyncer).GetHashCode();
        }

        public static void RequestDig(int cell, int animationDelay)
        {
            var s = Instance;
            if (s == null || ProcessingIncoming) return;
            try
            {
                var p = ToolMenu.Instance?.PriorityScreen?.GetLastSelectedPriority() ?? default;
                var originator = LocalUserIdQuery?.Invoke() ?? 0;
                if (MultiplayerSession.IsHost)
                    s.CallCommand(nameof(CmdDig), cell, animationDelay, (int)p.priority_class, p.priority_value, originator);
                else
                    s.CallCommand(nameof(CmdHostDig), cell, animationDelay, (int)p.priority_class, p.priority_value, originator);
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[DigToolSyncer] {ex}");
            }
        }

        [Command]
        private void CmdDig(int cell, int animationDelay, int priority_class, int priority_value, ulong originator)
        {
            CallClientRpc(nameof(RpcDig), cell, animationDelay, priority_class, priority_value, originator);
        }

        [Command]
        private void CmdHostDig(int cell, int animationDelay, int priority_class, int priority_value, ulong originator)
        {
            ProcessingIncoming = true;
            try { ApplyDig(cell, animationDelay, priority_class, priority_value); }
            finally { ProcessingIncoming = false; }
            CallClientRpc(nameof(RpcDig), cell, animationDelay, priority_class, priority_value, originator);
        }

        [ClientRpc]
        private void RpcDig(int cell, int animationDelay, int priority_class, int priority_value, ulong originator)
        {
            if (originator != 0 && originator == (LocalUserIdQuery?.Invoke() ?? 0)) return;
            ProcessingIncoming = true;
            try { ApplyDig(cell, animationDelay, priority_class, priority_value); }
            finally { ProcessingIncoming = false; }
        }

        private static void ApplyDig(int cell, int animationDelay, int priority_class, int priority_value)
        {
            var go = DigTool.PlaceDig(cell, animationDelay);
            var prioritizable = go?.GetComponent<Prioritizable>();
            if (prioritizable != null)
                prioritizable.SetMasterPriority(new PrioritySetting((PriorityScreen.PriorityClass)priority_class, priority_value));
        }
    }
}
