using HarmonyLib;
using ONI_Together.Networking;
using ONI_Together.Networking.Components;
using ONI_Together.Networking.OxySync.Components;
using ONI_Together.Networking.OxySync.Components.Tools;
using ONI_Together.Scripts.Creatures;
using Shared.Profiling;
using UnityEngine;

namespace ONI_Together.Patches.ToolPatches.Sandbox
{
    internal static class SandboxToolSync
    {
        public static void Send(byte action, int cell, int distanceFromOrigin = 0, Vector3 position = default)
        {
            using var _ = Profiler.Scope();
            if (!MultiplayerSession.InActiveSession || !Grid.IsValidCell(cell))
                return;
            var s = SandboxToolParameterMenu.instance?.settings;
            if (s == null) return;
            SandboxToolSyncer.RequestSandbox(action, cell, distanceFromOrigin, position, s.GetIntSetting(SandboxSettings.KEY_SELECTED_ELEMENT), s.GetIntSetting(SandboxSettings.KEY_DISEASE_COUNT), s.GetIntSetting(SandboxSettings.KEY_MORALE_ADJUSTMENT), s.GetFloatSetting(SandboxSettings.KEY_MASS), s.GetFloatSetting(SandboxSettings.KEY_TEMPERATURE), s.GetFloatSetting(SandboxSettings.KEY_TEMPERATURE_ADDITIVE), s.GetFloatSetting(SandboxSettings.KEY_STRESS_ADDITIVE), s.GetStringSetting(SandboxSettings.KEY_SELECTED_DISEASE) ?? string.Empty, s.GetStringSetting(SandboxSettings.KEY_SELECTED_ENTITY) ?? string.Empty, s.GetStringSetting(SandboxSettings.KEY_SELECTED_STORY) ?? string.Empty);
        }
    }

    [HarmonyPatch(typeof(SandboxBrushTool), nameof(SandboxBrushTool.OnPaintCell))]
    internal static class SandboxBrushToolPatch
    {
        private static void Postfix(int cell, int distFromOrigin) =>
            SandboxToolSync.Send(0, cell, distFromOrigin);
    }

    [HarmonyPatch(typeof(SandboxSprinkleTool), nameof(SandboxSprinkleTool.OnPaintCell))]
    internal static class SandboxSprinkleToolPatch
    {
        private static void Postfix(int cell, int distFromOrigin) =>
            SandboxToolSync.Send(1, cell, distFromOrigin);
    }

    [HarmonyPatch(typeof(SandboxFloodTool), nameof(SandboxFloodTool.PaintCell))]
    internal static class SandboxFloodToolPatch
    {
        private static void Postfix(int cell) => SandboxToolSync.Send(2, cell);
    }

    [HarmonyPatch(typeof(SandboxSampleTool), nameof(SandboxSampleTool.Sample))]
    internal static class SandboxSampleToolPatch
    {
        private static void Postfix(int cell) => SandboxToolSync.Send(3, cell);
    }

    [HarmonyPatch(typeof(SandboxHeatTool), nameof(SandboxHeatTool.OnPaintCell))]
    internal static class SandboxHeatToolPatch
    {
        private static void Postfix(int cell, int distFromOrigin) =>
            SandboxToolSync.Send(4, cell, distFromOrigin);
    }

    [HarmonyPatch(typeof(SandboxStressTool), nameof(SandboxStressTool.OnPaintCell))]
    internal static class SandboxStressToolPatch
    {
        private static void Postfix(int cell, int distFromOrigin) =>
            SandboxToolSync.Send(5, cell, distFromOrigin);
    }

    [HarmonyPatch(typeof(SandboxSpawnerTool), nameof(SandboxSpawnerTool.Place))]
    internal static class SandboxSpawnerToolPatch
    {
        public static bool IsPlacingEntity = false;
        public static GameObject LastSpawnedObject = null;

        private static bool Prefix(int cell)
        {
            using var _ = Profiler.Scope();
            if (!MultiplayerSession.InActiveSession || !Grid.IsValidCell(cell))
                return true;
            IsPlacingEntity = true;
            LastSpawnedObject = null;
            return true;
        }

        private static void Postfix(int cell)
        {
            using var _ = Profiler.Scope();
            try
            {
                if (MultiplayerSession.IsHost && Grid.IsValidCell(cell))
                {
                    GameObject spawned = LastSpawnedObject;
                    if (spawned == null)
                    {
                        spawned = Grid.Objects[cell, (int)Grid.SceneLayer.Creatures];
                        if (spawned == null)
                            spawned = Grid.Objects[cell, (int)Grid.SceneLayer.Building];
                        if (spawned == null)
                        {
                            foreach (var minion in global::Components.LiveMinionIdentities.Items)
                            {
                                if (minion != null && Grid.PosToCell(minion) == cell)
                                {
                                    spawned = minion.gameObject;
                                    break;
                                }
                            }
                        }
                    }
                    if (spawned != null)
                    {
                        var building = spawned.GetComponent<Building>();
                        if (building != null)
                        {
                            var def = building.Def;
                            var pe = spawned.GetComponent<PrimaryElement>();
                            var packet = new ONI_Together.Networking.Packets.Tools.Build.BuildCompletePacket
                            {
                                PrefabID = def.PrefabID,
                                Cell = cell,
                                Orientation = building.Orientation,
                                ObjectLayer = def.ObjectLayer,
                                Temperature = pe?.Temperature ?? def.Temperature,
                                WorkerNetId = 0,
                                MaterialTags = def.DefaultElements().ConvertAll(t => t.Name)
                            };
                            PacketSender.SendToAllClients(packet);
                            return;
                        }
                        var identity = spawned.AddOrGet<NetworkIdentity>();
                        if (identity.NetId == 0)
                            identity.RegisterIdentity();
                        var minionIdentity = spawned.GetComponent<MinionIdentity>();
                        if (minionIdentity != null)
                        {
                            spawned.AddOrGet<OxySyncEntityPositionHandler>();
                            spawned.AddOrGet<AnimStateSyncer>();
                            spawned.AddOrGet<Scripts.Duplicants.MinionMultiplayerInitializer>();
                            try
                            {
                                var personality = Db.Get().Personalities.TryGet(minionIdentity.personalityResourceId);
                                if (personality == null)
                                    personality = Db.Get().Personalities.TryGet(minionIdentity.personalityResourceId.ToString());
                                if (personality == null)
                                    personality = Db.Get().Personalities.resources.Find(p => p.Id == minionIdentity.personalityResourceId.ToString());
                                if (personality == null)
                                    personality = Db.Get().Personalities.resources[0];
                                var stats = new MinionStartingStats(personality);
                                stats.Name = minionIdentity.name;
                                stats.voiceIdx = minionIdentity.voiceIdx;
                                var entry = ONI_Together.Networking.Packets.Social.ImmigrantOptionEntry.FromGameDeliverable(stats);
                                var packet2 = new ONI_Together.Networking.Packets.World.TelepadEntitySpawnPacket
                                {
                                    NetId = identity.NetId,
                                    Pos = spawned.transform.position,
                                    EntityData = entry
                                };
                                PacketSender.SendToAllClients(packet2);
                                return;
                            }
                            catch { }
                            string personalityId = minionIdentity.personalityResourceId.IsValid ? minionIdentity.personalityResourceId.ToString() : "HASSAN";
                            var personalityFallback = Db.Get().Personalities.TryGet(new HashedString(personalityId)) ?? Db.Get().Personalities.TryGet(personalityId) ?? Db.Get().Personalities.resources[0];
                            string dupeName = minionIdentity.name;
                            string prefabData = $"Minion|{personalityFallback.Id}|{dupeName}|{minionIdentity.voiceIdx}";
                            int hash = spawned.PrefabID().GetHashCode();
                            var packet = new ONI_Together.Networking.Packets.World.SpawnPrefabPacket(identity.NetId, hash, spawned.transform.position, prefabData) { IsActive = spawned.activeSelf };
                            PacketSender.SendToAllClients(packet);
                            return;
                        }
                        if (spawned.GetComponent<CreatureBrain>() != null || spawned.HasTag(GameTags.Creature))
                        {
                            spawned.AddOrGet<OxySyncEntityPositionHandler>();
                            spawned.AddOrGet<AnimStateSyncer>();
                            spawned.AddOrGet<CreatureMultiplayerInitializer>();
                        }
                        if (identity.NetId != 0)
                        {
                            string prefabName = spawned.PrefabID().Name;
                            int hash = spawned.PrefabID().GetHashCode();
                            var packet = new ONI_Together.Networking.Packets.World.SpawnPrefabPacket(identity.NetId, hash, spawned.transform.position, prefabName) { IsActive = spawned.activeSelf };
                            PacketSender.SendToAllClients(packet);
                        }
                    }
                }
            }
            catch { }
            finally
            {
                IsPlacingEntity = false;
                LastSpawnedObject = null;
            }
        }
    }

    [HarmonyPatch(typeof(SandboxDestroyerTool), nameof(SandboxDestroyerTool.OnPaintCell))]
    internal static class SandboxDestroyerToolPatch
    {
        private static void Postfix(int cell, int distFromOrigin) =>
            SandboxToolSync.Send(7, cell, distFromOrigin);
    }

    [HarmonyPatch(typeof(SandboxFOWTool), nameof(SandboxFOWTool.OnPaintCell))]
    internal static class SandboxFowToolPatch
    {
        private static void Postfix(int cell, int distFromOrigin) =>
            SandboxToolSync.Send(8, cell, distFromOrigin);
    }

    [HarmonyPatch(typeof(SandboxClearFloorTool), nameof(SandboxClearFloorTool.OnPaintCell))]
    internal static class SandboxClearFloorToolPatch
    {
        private static void Postfix(int cell, int distFromOrigin) =>
            SandboxToolSync.Send(9, cell, distFromOrigin);
    }

    [HarmonyPatch(typeof(SandboxCritterTool), nameof(SandboxCritterTool.OnPaintCell))]
    internal static class SandboxCritterToolPatch
    {
        private static void Postfix(int cell, int distFromOrigin) =>
            SandboxToolSync.Send(10, cell, distFromOrigin);
    }

    [HarmonyPatch(typeof(SandboxStoryTraitTool), nameof(SandboxStoryTraitTool.OnLeftClickDown))]
    internal static class SandboxStoryTraitToolPatch
    {
        private static void Prefix(SandboxStoryTraitTool __instance, Vector3 cursor_pos)
        {
            if (__instance == null || __instance.isPlacingTemplate) return;
            int cell = Grid.PosToCell(cursor_pos);
            if (Grid.IsValidCell(cell) && __instance.GetError(cursor_pos, out _, out _) == null)
                SandboxToolSync.Send(11, cell, position: cursor_pos);
        }
    }
}
