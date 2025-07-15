using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc.World;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONI_MP.Misc
{
    public static class Utils
    {
        /// <summary>
        /// Max size of a single message that we can SEND.
        /// <para/>
        /// Note: We might be wiling to receive larger messages, and our peer might, too.
        /// </summary>
        public static int MaxSteamNetworkingSocketsMessageSizeSend = 512 * 1024;

        public static void LogHierarchy(Transform root, string prefix = "")
        {
            if (root == null)
            {
                DebugConsole.LogWarning("LogHierarchy called with null root.");
                return;
            }

            DebugConsole.Log($"{prefix}{root.name}");

            foreach (Transform child in root)
            {
                LogHierarchy(child, prefix + "  ");
            }
        }

        public static void Inject<T>(GameObject prefab) where T : KMonoBehaviour
        {
            if (prefab.GetComponent<T>() == null)
            {
                DebugConsole.Log($"Added {typeof(T).Name} to {prefab.name}");
                prefab.AddOrGet<T>();
            }
        }

        public static void InjectAll(GameObject prefab, params Type[] types)
        {
            foreach (var type in types)
            {
                if (!typeof(KMonoBehaviour).IsAssignableFrom(type)) continue;
                if (prefab.GetComponent(type) != null) continue;

                DebugConsole.Log($"Added {type.Name} to {prefab.name}");
                prefab.AddComponent(type);
            }
        }


        public static void ListAllTMPFonts()
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            DebugConsole.Log($"Found {fonts.Length} TMP_FontAsset(s):");

            foreach (var font in fonts)
            {
                DebugConsole.Log($" - {font.name} (path: {font.name}, instance ID: {font.GetInstanceID()})");
            }

            if (fonts.Length == 0)
            {
                DebugConsole.Log("No TMP_FontAsset found in memory.");
            }
        }

        public static TMP_FontAsset GetDefaultTMPFont()
        {
            return Localization.FontAsset;
        }

        public static List<ChunkData> CollectChunks(int startX, int startY, int chunkSize, int numChunksX, int numChunksY)
        {
            var chunks = new List<ChunkData>();
            for (int cx = 0; cx < numChunksX; cx++)
                for (int cy = 0; cy < numChunksY; cy++)
                {
                    int x0 = startX + cx * chunkSize;
                    int y0 = startY + cy * chunkSize;
                    chunks.Add(CreateChunk(x0, y0, chunkSize, chunkSize));
                }
            return chunks;
        }

        private static ChunkData CreateChunk(int x0, int y0, int width, int height)
        {
            var chunk = new ChunkData
            {
                TileX = x0,
                TileY = y0,
                Width = width,
                Height = height,
                Tiles = new ushort[width * height],
                Temperatures = new float[width * height],
                Masses = new float[width * height],
                DiseaseIdx = new byte[width * height],
                DiseaseCount = new int[width * height],
            };

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    int x = x0 + i, y = y0 + j;
                    int idx = i + j * width;
                    int cell = Grid.XYToCell(x, y);

                    if (!Grid.IsValidCell(cell)) continue;

                    chunk.Tiles[idx] = Grid.ElementIdx[cell];
                    chunk.Temperatures[idx] = Grid.Temperature[cell];
                    chunk.Masses[idx] = Grid.Mass[cell];
                    chunk.DiseaseIdx[idx] = Grid.DiseaseIdx[cell];
                    chunk.DiseaseCount[idx] = Grid.DiseaseCount[cell];
                }

            return chunk;
        }

        [Obsolete("Use new FormatBytes instead!")]
        public static string FormatBytesOld(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / 1024f / 1024f:F2} MB";
        }

        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Formats a timespan nicely:
        /// 45s, 3m 12s, 1h 15m 20s
        /// </summary>
        public static string FormatTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);

            string result = "";

            if (ts.Hours > 0)
                result += $"{ts.Hours}h ";

            if (ts.Minutes > 0 || ts.Hours > 0)
                result += $"{ts.Minutes}m ";

            result += $"{ts.Seconds}s";

            return result.Trim();
        }

        public static bool IsInMenu()
        {
            return App.GetCurrentSceneName() == "frontend";
        }

        public static bool IsInGame()
        {
            return App.GetCurrentSceneName() == "backend";
        }

        public static GameObject FindNearbyWorkable(Vector3 position, float radius, Predicate<GameObject> predicate)
        {
            foreach (Workable workable in UnityEngine.Object.FindObjectsOfType<Workable>())
            {
                if (workable == null) continue;

                var go = workable.gameObject;
                float dist = Vector3.Distance(go.transform.position, position);

                if (dist <= radius && predicate(go))
                    return go;
            }

            return null;
        }

        public static GameObject FindClosestGameObjectWithTag(Vector3 position, Tag tag, float radius)
        {
            GameObject closest = null;
            float closestDistSq = radius * radius;

            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
            {
                if (!go.HasTag(tag))
                    continue;

                float distSq = (go.transform.position - position).sqrMagnitude;
                if (distSq < closestDistSq)
                {
                    closest = go;
                    closestDistSq = distSq;
                }
            }

            return closest;
        }

        public static GameObject FindEntityInRadius(Vector3 origin, float radius, Predicate<GameObject> predicate)
        {
            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
            {
                if (go == null) continue;

                float dist = (go.transform.position - origin).sqrMagnitude;
                if (dist <= radius * radius && predicate(go))
                    return go;
            }

            return null;
        }

        #region SaveLoadRoot Extensions
        private static readonly FieldInfo optionalComponentListField =
            typeof(SaveLoadRoot).GetField("m_optionalComponentTypeNames", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void TryDeclareOptionalComponent<T>(this SaveLoadRoot root) where T : KMonoBehaviour
        {
            if (optionalComponentListField?.GetValue(root) is List<string> list)
            {
                string typeName = typeof(T).ToString();

                if (!list.Contains(typeName))
                {
                    root.DeclareOptionalComponent<T>();
                }
            }
            else
            {
                DebugConsole.LogWarning("Could not access m_optionalComponentTypeNames via reflection.");
            }
        }
        #endregion

        #region KBatchedAnimEventToggler Extensions
        public static void Trigger(this KBatchedAnimEventToggler toggler, int eventHash, bool enable)
        {
            if (enable)
                toggler.SendMessage("Enable", null, SendMessageOptions.DontRequireReceiver);
            else
                toggler.SendMessage("Disable", null, SendMessageOptions.DontRequireReceiver);
        }
        #endregion

        #region Grid Extensions
        public static bool IsWalkableCell(int cell)
        {
            return Grid.IsValidCell(cell)
                && !Grid.Solid[cell]
                && !Grid.DupeImpassable[cell]
                && Grid.DupePassable[cell];
        }
        #endregion
    }
}
