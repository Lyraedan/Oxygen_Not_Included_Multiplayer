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
                var go = new GameObject("DigToolSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<DigToolSyncer>();
            }
            Instance.NetId = nameof(DigToolSyncer).GetHashCode();
        }

        public static void RequestDig(int cell, int animationDelay)
        {
            var s = Instance;
            if (s == null) return;
            try
            {
                var p = ToolMenu.Instance?.PriorityScreen?.GetLastSelectedPriority() ?? default;
                s.CallCommand(nameof(CmdDig), cell, animationDelay, (int)p.priority_class, p.priority_value);
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[DigToolSyncer] {ex}");
            }
        }

        [Command]
        private void CmdDig(int cell, int animationDelay, int priority_class, int priority_value)
        {
            CallClientRpc(nameof(RpcDig), cell, animationDelay, priority_class, priority_value);
        }

        [ClientRpc]
        private void RpcDig(int cell, int animationDelay, int priority_class, int priority_value)
        {
            if (_processing) return;
            _processing = true;
            try
            {
                var go = DigTool.PlaceDig(cell, animationDelay);
                var prioritizable = go?.GetComponent<Prioritizable>();
                if (prioritizable != null)
                    prioritizable.SetMasterPriority(new PrioritySetting((PriorityScreen.PriorityClass)priority_class, priority_value));
            }
            finally
            {
                _processing = false;
            }
        }
    }
}
