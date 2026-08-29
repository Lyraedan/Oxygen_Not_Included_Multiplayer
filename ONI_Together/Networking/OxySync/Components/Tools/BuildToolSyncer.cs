using System.Collections.Generic;
using System.Linq;
using ONI_Together.DebugTools;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.Components.Tools
{
    [SkipSaveFileSerialization]
    [FixedInterestGroup]
    public class BuildToolSyncer : NetworkBehaviour
    {
        public static BuildToolSyncer Instance { get; private set; }
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
                var go = new GameObject("BuildToolSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<BuildToolSyncer>();
            }
            Instance.NetId = nameof(BuildToolSyncer).GetHashCode();
        }

        public static void RequestBuild(string prefabID, int cell, int orientation, List<string> materialTags, int objectLayer, bool instantBuild)
        {
            var s = Instance;
            if (s == null) return;
            try
            {
                var p = PlanScreen.Instance != null ? PlanScreen.Instance.GetBuildingPriority() : ToolMenu.Instance?.PriorityScreen?.GetLastSelectedPriority() ?? default;
                s.CallCommand(nameof(CmdBuild), prefabID, cell, orientation, materialTags, (int)p.priority_class, p.priority_value, objectLayer, instantBuild);
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogWarning($"[BuildToolSyncer] {ex}");
            }
        }

        [Command]
        private void CmdBuild(string prefabID, int cell, int orientation, List<string> materialTags, int priority_class, int priority_value, int objectLayer, bool instantBuild)
        {
            CallClientRpc(nameof(RpcBuild), prefabID, cell, orientation, materialTags, priority_class, priority_value, objectLayer, instantBuild);
        }

        [ClientRpc]
        private void RpcBuild(string prefabID, int cell, int orientation, List<string> materialTags, int priority_class, int priority_value, int objectLayer, bool instantBuild)
        {
            if (_processing) return;
            if (!Grid.IsValidCell(cell)) return;
            var def = Assets.GetBuildingDef(prefabID);
            if (def == null) return;
            _processing = true;
            try
            {
                var tags = materialTags.Select(t => TagManager.Create(t)).ToList();
                var pos = Grid.CellToPosCBC(cell, Grid.SceneLayer.Building);
                GameObject built = null;
                if (instantBuild)
                    built = BuildInternal(def, tags, pos, (Orientation)orientation, cell);
                else
                    built = QueueBuild(def, tags, pos, (Orientation)orientation);
                if (built == null && def.ReplacementLayer != ObjectLayer.NumLayers)
                    built = HandleReplacement(def, pos, tags, (Orientation)orientation, cell, instantBuild) ?? built;
                var pri = built?.GetComponent<Prioritizable>();
                if (pri != null)
                    pri.SetMasterPriority(new PrioritySetting((PriorityScreen.PriorityClass)priority_class, priority_value));
            }
            finally
            {
                _processing = false;
            }
        }

        private static GameObject QueueBuild(BuildingDef def, List<Tag> tags, Vector3 pos, Orientation orientation)
        {
            var viz = Util.KInstantiate(def.BuildingPreview, pos);
            return def.TryPlace(viz, pos, orientation, tags, "DEFAULT_FACADE");
        }

        private static GameObject BuildInternal(BuildingDef def, List<Tag> tags, Vector3 pos, Orientation orientation, int cell)
        {
            if (!def.IsValidBuildLocation(null, pos, orientation) || !def.IsValidPlaceLocation(null, pos, orientation, out _))
                return null;
            if (def.ObjectLayer == ObjectLayer.Building)
            {
                def.RunOnArea(cell, orientation, c =>
                {
                    if (Uprootable.CanUproot(Grid.Objects[c, (int)def.ObjectLayer], out var u))
                        u.CompleteWork(null);
                });
            }
            else if (def.ObjectLayer == ObjectLayer.Backwall)
            {
                def.RunOnArea(cell, orientation, c =>
                {
                    if (BackwallManager.HasBackwall(c))
                        SimMessages.Dig(c, -1, skipEvent: true, backwall: true);
                });
            }
            float temp = Mathf.Min(def.Temperature, ElementLoader.GetMinMeltingPointAmongElements(tags) - 10f);
            return def.Build(cell, orientation, null, tags, temp, "DEFAULT_FACADE", false, GameClock.Instance.GetTime());
        }

        private static GameObject HandleReplacement(BuildingDef def, Vector3 pos, List<Tag> tags, Orientation orientation, int cell, bool instant)
        {
            var cand = def.GetReplacementCandidate(cell);
            if (cand == null || def.IsReplacementLayerOccupied(cell)) return null;
            var comp = cand.GetComponent<BuildingComplete>();
            if (comp == null || !comp.Def.Replaceable || !def.CanReplace(cand)) return null;
            var tag = cand.GetComponent<PrimaryElement>().Element.tag;
            if (tag.GetHash() == (int)SimHashes.StableSnow) tag = SimHashes.Snow.CreateTag();
            if (comp.Def == def && tags.Count > 0 && tags[0] == tag) return null;
            var viz = Util.KInstantiate(def.BuildingPreview, pos);
            if (!instant)
            {
                var r = def.TryReplaceTile(viz, pos, orientation, tags, "DEFAULT_FACADE");
                if (r != null) Grid.Objects[cell, (int)def.ReplacementLayer] = r;
                return r;
            }
            if (!def.IsValidBuildLocation(null, pos, orientation, true) || !def.IsValidPlaceLocation(null, pos, orientation, true, out _)) return null;
            float temp = Mathf.Min(def.Temperature, ElementLoader.GetMinMeltingPointAmongElements(tags) - 10f);
            return def.Build(cell, orientation, null, tags, temp, "DEFAULT_FACADE", false, GameClock.Instance.GetTime());
        }
    }
}
