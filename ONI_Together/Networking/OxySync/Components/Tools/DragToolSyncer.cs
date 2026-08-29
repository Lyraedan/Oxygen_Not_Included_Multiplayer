using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ONI_Together.DebugTools;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.Components.Tools
{
    [SkipSaveFileSerialization]
    [FixedInterestGroup]
    public class DragToolSyncer : NetworkBehaviour
    {
        public static DragToolSyncer Instance { get; private set; }

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
                var go = new GameObject("DragToolSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<DragToolSyncer>();
            }
            Instance.NetId = nameof(DragToolSyncer).GetHashCode();
        }

        public static void RequestDrag(string toolName, int cell, int distFromOrigin)
        {
            var s = Instance;
            if (s == null) return;
            try
            {
                DragTool tool = ResolveTool(toolName);
                List<string> filters = new();
                if (tool is FilteredDragTool filtered)
                {
                    foreach (var t in filtered.currentFilters)
                        if (t.state == ToolParameterMenu.ToggleState.On)
                            filters.Add(t.name);
                }
                var p = ToolMenu.Instance?.PriorityScreen?.GetLastSelectedPriority() ?? default;
                s.CallCommand(nameof(CmdDrag), toolName, cell, distFromOrigin, filters, (int)p.priority_class, p.priority_value, 0, Vector3.zero, Vector3.zero);
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[DragToolSyncer] {ex}");
            }
        }

        public static void RequestDragComplete(string toolName, Vector3 downPos, Vector3 upPos)
        {
            var s = Instance;
            if (s == null) return;
            try
            {
                DragTool tool = ResolveTool(toolName);
                List<string> filters = new();
                if (tool is FilteredDragTool filtered)
                {
                    foreach (var t in filtered.currentFilters)
                        if (t.state == ToolParameterMenu.ToggleState.On)
                            filters.Add(t.name);
                }
                var p = ToolMenu.Instance?.PriorityScreen?.GetLastSelectedPriority() ?? default;
                s.CallCommand(nameof(CmdDrag), toolName, 0, 0, filters, (int)p.priority_class, p.priority_value, 1, downPos, upPos);
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[DragToolSyncer] {ex}");
            }
        }

        private static DragTool ResolveTool(string name)
        {
            return name switch
            {
                "Cancel" => CancelTool.Instance,
                "Clear" => ClearTool.Instance,
                "Deconstruct" => DeconstructTool.Instance,
                "Mop" => MopTool.Instance,
                "Harvest" => HarvestTool.Instance,
                "Disinfect" => DisinfectTool.Instance,
                "Prioritize" => PrioritizeTool.Instance,
                "EmptyPipe" => EmptyPipeTool.Instance,
                "Disconnect" => DisconnectTool.Instance,
                _ => null
            };
        }

        [Command]
        private void CmdDrag(string toolName, int cell, int distFromOrigin, List<string> filterTargets, int priority_class, int priority_value, int mode, Vector3 downPos, Vector3 upPos)
        {
            CallClientRpc(nameof(RpcDrag), toolName, cell, distFromOrigin, filterTargets, priority_class, priority_value, mode, downPos, upPos);
        }

        [ClientRpc]
        private void RpcDrag(string toolName, int cell, int distFromOrigin, List<string> filterTargets, int priority_class, int priority_value, int mode, Vector3 downPos, Vector3 upPos)
        {
            DragTool tool = ResolveTool(toolName);
            if (tool == null) return;
            FilteredDragTool filtered = tool as FilteredDragTool;
            bool isFiltered = filtered != null;
            HashSet<string> cached = new();
            if (isFiltered)
            {
                cached = filtered.currentFilters.Where(t => t.state == ToolParameterMenu.ToggleState.On).Select(t => t.name).ToHashSet();
                foreach (var toggle in filtered.currentFilters)
                    toggle.state = ToolParameterMenu.ToggleState.Off;
                foreach (var target in filterTargets)
                    foreach (var toggle in filtered.currentFilters)
                        if (toggle.name == target) { toggle.state = ToolParameterMenu.ToggleState.On; break; }
            }
            var ps = ToolMenu.Instance?.PriorityScreen;
            PrioritySetting prev = default;
            bool hasPs = ps != null;
            if (hasPs)
            {
                prev = ps.lastSelectedPriority;
                var tr = Traverse.Create(ps).Field("lastSelectedPriority");
                if (tr != null) tr.SetValue(new PrioritySetting((PriorityScreen.PriorityClass)priority_class, priority_value));
                else ps.lastSelectedPriority = new PrioritySetting((PriorityScreen.PriorityClass)priority_class, priority_value);
            }
            Vector3 cachedDown = tool.downPos;
            bool completed = false;
            try
            {
                if (mode == 0)
                    tool.OnDragTool(cell, distFromOrigin);
                else
                {
                    tool.downPos = downPos;
                    tool.OnDragComplete(downPos, upPos);
                }
                completed = true;
            }
            finally
            {
                tool.downPos = cachedDown;
                if (hasPs)
                {
                    var tr2 = Traverse.Create(ps).Field("lastSelectedPriority");
                    if (tr2 != null) tr2.SetValue(prev);
                    else ps.lastSelectedPriority = prev;
                }
                if (isFiltered)
                {
                    foreach (var toggle in filtered.currentFilters)
                        toggle.state = ToolParameterMenu.ToggleState.Off;
                    foreach (var target in cached)
                        foreach (var toggle in filtered.currentFilters)
                            if (toggle.name == target) { toggle.state = ToolParameterMenu.ToggleState.On; break; }
                }
            }
        }
    }
}
