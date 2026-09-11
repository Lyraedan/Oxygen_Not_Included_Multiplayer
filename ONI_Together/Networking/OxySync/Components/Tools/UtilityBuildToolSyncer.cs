using System.Collections.Generic;
using System.Linq;
using ONI_Together.DebugTools;
using ONI_Together.Misc;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.Components.Tools
{
    [SkipSaveFileSerialization]
    [FixedInterestGroup]
    public class UtilityBuildToolSyncer : NetworkBehaviour
    {
        public static UtilityBuildToolSyncer Instance { get; private set; }

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
                var go = new GameObject("UtilityBuildToolSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<UtilityBuildToolSyncer>();
            }
            Instance.NetId = nameof(UtilityBuildToolSyncer).GetHashCode();
        }

        public static void RequestUtilityBuild(string prefabID, List<string> materialTags, ulong[] pathChunks, string facadeID, bool instantBuild)
        {
            var s = Instance;
            if (s == null || ProcessingIncoming) return;
            try
            {
                var p = PlanScreen.Instance != null ? PlanScreen.Instance.GetBuildingPriority() : ToolMenu.Instance?.PriorityScreen?.GetLastSelectedPriority() ?? default;
                var originator = LocalUserIdQuery?.Invoke() ?? 0;
                if (MultiplayerSession.IsHost)
                    s.CallCommand(nameof(CmdUtilityBuild), prefabID, materialTags, pathChunks, facadeID, instantBuild, (int)p.priority_class, p.priority_value, originator);
                else
                    s.CallCommand(nameof(CmdHostUtilityBuild), prefabID, materialTags, pathChunks, facadeID, instantBuild, (int)p.priority_class, p.priority_value, originator);
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[UtilityBuildToolSyncer] {ex}");
            }
        }

        [Command]
        private void CmdUtilityBuild(string prefabID, List<string> materialTags, ulong[] pathChunks, string facadeID, bool instantBuild, int priority_class, int priority_value, ulong originator)
        {
            CallClientRpc(nameof(RpcUtilityBuild), prefabID, materialTags, pathChunks, facadeID, instantBuild, priority_class, priority_value, originator);
        }

        [Command]
        private void CmdHostUtilityBuild(string prefabID, List<string> materialTags, ulong[] pathChunks, string facadeID, bool instantBuild, int priority_class, int priority_value, ulong originator)
        {
            ProcessingIncoming = true;
            try { ApplyUtilityBuild(prefabID, materialTags, pathChunks, facadeID, instantBuild, priority_class, priority_value); }
            finally { ProcessingIncoming = false; }
            CallClientRpc(nameof(RpcUtilityBuild), prefabID, materialTags, pathChunks, facadeID, instantBuild, priority_class, priority_value, originator);
        }

        [ClientRpc]
        private void RpcUtilityBuild(string prefabID, List<string> materialTags, ulong[] pathChunks, string facadeID, bool instantBuild, int priority_class, int priority_value, ulong originator)
        {
            if (originator != 0 && originator == (LocalUserIdQuery?.Invoke() ?? 0)) return;
            ProcessingIncoming = true;
            try { ApplyUtilityBuild(prefabID, materialTags, pathChunks, facadeID, instantBuild, priority_class, priority_value); }
            finally { ProcessingIncoming = false; }
        }

        private static void ApplyUtilityBuild(string prefabID, List<string> materialTags, ulong[] pathChunks, string facadeID, bool instantBuild, int priority_class, int priority_value)
        {
            if (pathChunks == null || pathChunks.Length == 0) return;
            List<BaseUtilityBuildTool.PathNode> path = new();
            for (int c = 0; c < pathChunks.Length; c++)
            {
                ulong chunk = pathChunks[c];
                int[] cells = BuildingUtils.DecodeUtilityPathChunk((uint)(chunk & 0xFFFFFFFF));
                if (cells == null) continue;
                uint mask = (uint)(chunk >> 32);
                for (int j = 0; j < cells.Length; j++)
                    path.Add(new BaseUtilityBuildTool.PathNode { cell = cells[j], valid = (mask & (1u << j)) != 0 });
            }
            if (path.Count == 0) return;
            var def = Assets.GetBuildingDef(prefabID);
            if (def == null) return;
            var tags = materialTags.Select(t => TagManager.Create(t)).ToList();
            if (tags.Count == 0) tags.AddRange(def.DefaultElements());
            BaseUtilityBuildTool tool = def.BuildingComplete.TryGetComponent<Wire>(out _) ? (BaseUtilityBuildTool)WireBuildTool.Instance : UtilityBuildTool.Instance;
            if (PlanScreen.Instance?.ProductInfoScreen?.materialSelectionPanel?.PriorityScreen == null)
            {
                PlanScreen.Instance.CopyBuildingOrder(def, facadeID);
                PlanScreen.Instance.OnActiveToolChanged(SelectTool.Instance);
            }
            var cachedDef = tool.def;
            var cachedPath = tool.path != null ? new List<BaseUtilityBuildTool.PathNode>(tool.path) : new List<BaseUtilityBuildTool.PathNode>();
            var cachedMats = tool.selectedElements != null ? new List<Tag>(tool.selectedElements) : new List<Tag>();
            var cachedMgr = tool.conduitMgr;
            var haver = def.BuildingComplete.GetComponent<IHaveUtilityNetworkMgr>();
            tool.def = def;
            tool.path = path;
            tool.selectedElements = tags;
            tool.conduitMgr = haver.GetNetworkManager();
            bool cachedInstant = DebugHandler.InstantBuildMode;
            DebugHandler.InstantBuildMode = instantBuild;
            try
            {
                tool.BuildPath();
                foreach (var node in path)
                {
                    var go = Grid.Objects[node.cell, (int)def.TileLayer];
                    if (go == null) continue;
                    if (go.TryGetComponent<Prioritizable>(out var pri)) pri.SetMasterPriority(new PrioritySetting((PriorityScreen.PriorityClass)priority_class, priority_value));
                    if (go.TryGetComponent<KAnimGraphTileVisualizer>(out var vis)) { vis.UpdateConnections(vis.Connections); vis.Refresh(); }
                }
            }
            finally
            {
                DebugHandler.InstantBuildMode = cachedInstant;
                tool.def = cachedDef;
                tool.path = cachedPath;
                tool.selectedElements = cachedMats;
                tool.conduitMgr = cachedMgr;
            }
        }
    }
}
