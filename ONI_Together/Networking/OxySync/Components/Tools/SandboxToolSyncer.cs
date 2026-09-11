using ONI_Together.DebugTools;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.Components.Tools
{
    [SkipSaveFileSerialization]
    [FixedInterestGroup]
    public class SandboxToolSyncer : NetworkBehaviour
    {
        public static SandboxToolSyncer Instance { get; private set; }

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
                var go = new GameObject("SandboxToolSyncer");
                go.transform.SetParent(root.transform);
                Instance = go.AddComponent<SandboxToolSyncer>();
            }
            Instance.NetId = nameof(SandboxToolSyncer).GetHashCode();
        }

        public static void RequestSandbox(byte action, int cell, int dist, Vector3 pos, int elementIndex, int diseaseCount, int moraleAdj, float mass, float temp, float tempAdd, float stressAdd, string diseaseId, string entityId, string storyId)
        {
            var s = Instance;
            if (s == null || ProcessingIncoming) return;
            try
            {
                var originator = LocalUserIdQuery?.Invoke() ?? 0;
                if (MultiplayerSession.IsHost)
                    s.CallCommand(nameof(CmdSandbox), action, cell, dist, pos, elementIndex, diseaseCount, moraleAdj, mass, temp, tempAdd, stressAdd, diseaseId, entityId, storyId, originator);
                else
                    s.CallCommand(nameof(CmdHostSandbox), action, cell, dist, pos, elementIndex, diseaseCount, moraleAdj, mass, temp, tempAdd, stressAdd, diseaseId, entityId, storyId, originator);
            }
            catch (System.Exception ex) { DebugConsole.LogWarning($"[SandboxToolSyncer] {ex}"); }
        }

        [Command]
        private void CmdSandbox(byte action, int cell, int dist, Vector3 pos, int elementIndex, int diseaseCount, int moraleAdj, float mass, float temp, float tempAdd, float stressAdd, string diseaseId, string entityId, string storyId, ulong originator)
        {
            CallClientRpc(nameof(RpcSandbox), action, cell, dist, pos, elementIndex, diseaseCount, moraleAdj, mass, temp, tempAdd, stressAdd, diseaseId, entityId, storyId, originator);
        }

        [Command]
        private void CmdHostSandbox(byte action, int cell, int dist, Vector3 pos, int elementIndex, int diseaseCount, int moraleAdj, float mass, float temp, float tempAdd, float stressAdd, string diseaseId, string entityId, string storyId, ulong originator)
        {
            ProcessingIncoming = true;
            try { ApplySandbox(action, cell, dist, pos, elementIndex, diseaseCount, moraleAdj, mass, temp, tempAdd, stressAdd, diseaseId, entityId, storyId); }
            finally { ProcessingIncoming = false; }
            CallClientRpc(nameof(RpcSandbox), action, cell, dist, pos, elementIndex, diseaseCount, moraleAdj, mass, temp, tempAdd, stressAdd, diseaseId, entityId, storyId, originator);
        }

        [ClientRpc]
        private void RpcSandbox(byte action, int cell, int dist, Vector3 pos, int elementIndex, int diseaseCount, int moraleAdj, float mass, float temp, float tempAdd, float stressAdd, string diseaseId, string entityId, string storyId, ulong originator)
        {
            if (originator != 0 && originator == (LocalUserIdQuery?.Invoke() ?? 0)) return;
            ProcessingIncoming = true;
            try { ApplySandbox(action, cell, dist, pos, elementIndex, diseaseCount, moraleAdj, mass, temp, tempAdd, stressAdd, diseaseId, entityId, storyId); }
            finally { ProcessingIncoming = false; }
        }

        private static void ApplySandbox(byte action, int cell, int dist, Vector3 pos, int elementIndex, int diseaseCount, int moraleAdj, float mass, float temp, float tempAdd, float stressAdd, string diseaseId, string entityId, string storyId)
        {
            if (!Grid.IsValidCell(cell) || SandboxToolParameterMenu.instance?.settings == null) return;
            var settings = SandboxToolParameterMenu.instance.settings;
            int prevElem = 0, prevDisease = 0, prevMorale = 0;
            float prevMass = 0, prevTemp = 0, prevTempAdd = 0, prevStress = 0;
            string prevDiseaseId = "", prevEntityId = "", prevStoryId = "";
            bool isBrush = action == 0 || action == 1 || action == 2;
            bool isHeat = action == 4;
            bool isStress = action == 5;
            bool isEntity = action == 6;
            bool isStory = action == 11;
            try
            {
                if (isBrush)
                {
                    prevElem = settings.GetIntSetting(SandboxSettings.KEY_SELECTED_ELEMENT);
                    prevDisease = settings.GetIntSetting(SandboxSettings.KEY_DISEASE_COUNT);
                    prevMass = settings.GetFloatSetting(SandboxSettings.KEY_MASS);
                    prevTemp = settings.GetFloatSetting(SandboxSettings.KEY_TEMPERATURE);
                    prevDiseaseId = settings.GetStringSetting(SandboxSettings.KEY_SELECTED_DISEASE);
                    settings.SetIntSetting(SandboxSettings.KEY_SELECTED_ELEMENT, elementIndex);
                    settings.SetIntSetting(SandboxSettings.KEY_DISEASE_COUNT, diseaseCount);
                    settings.SetFloatSetting(SandboxSettings.KEY_MASS, mass);
                    settings.SetFloatSetting(SandboxSettings.KEY_TEMPERATURE, temp);
                    settings.SetStringSetting(SandboxSettings.KEY_SELECTED_DISEASE, diseaseId);
                }
                if (isHeat)
                {
                    prevTempAdd = settings.GetFloatSetting(SandboxSettings.KEY_TEMPERATURE_ADDITIVE);
                    settings.SetFloatSetting(SandboxSettings.KEY_TEMPERATURE_ADDITIVE, tempAdd);
                }
                if (isStress)
                {
                    prevStress = settings.GetFloatSetting(SandboxSettings.KEY_STRESS_ADDITIVE);
                    prevMorale = settings.GetIntSetting(SandboxSettings.KEY_MORALE_ADJUSTMENT);
                    settings.SetFloatSetting(SandboxSettings.KEY_STRESS_ADDITIVE, stressAdd);
                    settings.SetIntSetting(SandboxSettings.KEY_MORALE_ADJUSTMENT, moraleAdj);
                }
                if (isEntity)
                {
                    prevEntityId = settings.GetStringSetting(SandboxSettings.KEY_SELECTED_ENTITY);
                    settings.SetStringSetting(SandboxSettings.KEY_SELECTED_ENTITY, entityId);
                }
                if (isStory)
                {
                    prevStoryId = settings.GetStringSetting(SandboxSettings.KEY_SELECTED_STORY);
                    settings.SetStringSetting(SandboxSettings.KEY_SELECTED_STORY, storyId);
                }
                switch (action)
                {
                    case 0:
                        FindTool(SandboxBrushTool.instance)?.OnPaintCell(cell, dist);
                        break;
                    case 1:
                        FindTool(SandboxSprinkleTool.instance)?.OnPaintCell(cell, dist);
                        break;
                    case 2:
                        FindTool(SandboxFloodTool.instance)?.PaintCell(cell);
                        break;
                    case 3:
                        settings.SetIntSetting(SandboxSettings.KEY_SELECTED_ELEMENT, elementIndex);
                        settings.SetIntSetting(SandboxSettings.KEY_DISEASE_COUNT, diseaseCount);
                        settings.SetFloatSetting(SandboxSettings.KEY_MASS, mass);
                        settings.SetFloatSetting(SandboxSettings.KEY_TEMPERATURE, temp);
                        settings.SetStringSetting(SandboxSettings.KEY_SELECTED_DISEASE, diseaseId);
                        SandboxToolParameterMenu.instance.RefreshDisplay();
                        break;
                    case 4:
                        FindTool(SandboxHeatTool.instance)?.OnPaintCell(cell, dist);
                        break;
                    case 5:
                        FindTool(SandboxStressTool.instance)?.OnPaintCell(cell, dist);
                        break;
                    case 6:
                        var spawner = Object.FindFirstObjectByType<SandboxSpawnerTool>(FindObjectsInactive.Include);
                        if (spawner != null)
                        {
                            int prev = spawner.currentCell;
                            spawner.currentCell = cell;
                            try { spawner.Place(cell); } finally { spawner.currentCell = prev; }
                        }
                        break;
                    case 7:
                        FindTool(SandboxDestroyerTool.instance)?.OnPaintCell(cell, dist);
                        break;
                    case 8:
                        FindTool(SandboxFOWTool.instance)?.OnPaintCell(cell, dist);
                        break;
                    case 9:
                        FindTool(SandboxClearFloorTool.instance)?.OnPaintCell(cell, dist);
                        break;
                    case 10:
                        FindTool(SandboxCritterTool.instance)?.OnPaintCell(cell, dist);
                        break;
                    case 11:
                        var storyTool = Object.FindFirstObjectByType<SandboxStoryTraitTool>(FindObjectsInactive.Include);
                        storyTool?.OnLeftClickDown(pos);
                        break;
                }
            }
            finally
            {
                if (isBrush)
                {
                    settings.SetIntSetting(SandboxSettings.KEY_SELECTED_ELEMENT, prevElem);
                    settings.SetIntSetting(SandboxSettings.KEY_DISEASE_COUNT, prevDisease);
                    settings.SetFloatSetting(SandboxSettings.KEY_MASS, prevMass);
                    settings.SetFloatSetting(SandboxSettings.KEY_TEMPERATURE, prevTemp);
                    settings.SetStringSetting(SandboxSettings.KEY_SELECTED_DISEASE, prevDiseaseId);
                }
                if (isHeat) settings.SetFloatSetting(SandboxSettings.KEY_TEMPERATURE_ADDITIVE, prevTempAdd);
                if (isStress)
                {
                    settings.SetFloatSetting(SandboxSettings.KEY_STRESS_ADDITIVE, prevStress);
                    settings.SetIntSetting(SandboxSettings.KEY_MORALE_ADJUSTMENT, prevMorale);
                }
                if (isEntity) settings.SetStringSetting(SandboxSettings.KEY_SELECTED_ENTITY, prevEntityId);
                if (isStory) settings.SetStringSetting(SandboxSettings.KEY_SELECTED_STORY, prevStoryId);
            }
        }

        private static T FindTool<T>(T instance) where T : Object
        {
            return instance != null ? instance : Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        }
    }
}
