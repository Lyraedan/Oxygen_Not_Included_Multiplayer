using ONI_MP.DebugTools;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ONI_MP.Misc
{
	public static class ResourceLoader
	{
		public static Texture2D LoadEmbeddedTexture(string resourceName)
		{
			var assembly = Assembly.GetExecutingAssembly();
			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null)
				{
					DebugConsole.LogError($"Embedded resource not found: {resourceName}");
					return null;
				}

				byte[] buffer = new byte[stream.Length];
				stream.Read(buffer, 0, buffer.Length);

				Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
				texture.LoadImage(buffer);
				return texture;
			}
		}

		public static AssetBundle LoadEmbeddedAssetBundle(string resourceName)
		{
			var assembly = Assembly.GetExecutingAssembly();
			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null)
				{
					DebugConsole.LogError($"Embedded resource not found: {resourceName}");
					return null;
				}

				byte[] buffer = new byte[stream.Length];
				stream.Read(buffer, 0, buffer.Length);

				AssetBundle bundle = AssetBundle.LoadFromMemory(buffer);
				if (bundle == null)
				{
					DebugConsole.LogError($"Failed to load AssetBundle from resource: {resourceName}");
				}
				return bundle;
			}
		}

		public static Shader LoadShaderFromBundle(string bundleKey, string shaderName)
		{
			if (!MultiplayerMod.LoadedBundles.TryGetValue(bundleKey, out var bundle))
			{
				DebugConsole.LogError($"LoadShaderFromBundle: AssetBundle with key '{bundleKey}' is not loaded!");
				return null;
			}

			Shader shader = bundle.LoadAsset<Shader>(shaderName);
			if (shader == null)
			{
				DebugConsole.LogError($"LoadShaderFromBundle: Shader '{shaderName}' not found in bundle '{bundleKey}'!");
			}
			else
			{
				DebugConsole.Log($"LoadShaderFromBundle: Successfully loaded shader '{shaderName}' from bundle '{bundleKey}'.");
			}

			return shader;
		}

        public static GameObject LoadGameObjectFromBundle(string bundleKey, string prefabName)
        {
            if (!MultiplayerMod.LoadedBundles.TryGetValue(bundleKey, out var bundle))
            {
                DebugConsole.LogError($"LoadGameObjectFromBundle: AssetBundle with key '{bundleKey}' is not loaded!");
                return null;
            }

            GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
            if (prefab == null)
            {
                DebugConsole.LogError($"LoadGameObjectFromBundle: GameObject '{prefabName}' not found in bundle '{bundleKey}'!");
            }
            else
            {
                DebugConsole.Log($"LoadGameObjectFromBundle: Successfully loaded GameObject '{prefabName}' from bundle '{bundleKey}'.");
            }

            return prefab;
        }

        public static GameObject InstantiateGameObjectFromBundle(string bundleKey, string prefabName, Transform parent = null, Vector3? position = null, Quaternion? rotation = null)
        {
            GameObject prefab = LoadGameObjectFromBundle(bundleKey, prefabName);
            if (prefab == null)
                return null;

            GameObject instance = Object.Instantiate(
                prefab,
                position ?? Vector3.zero,
                rotation ?? Quaternion.identity,
                parent
            );

            return instance;
        }


    }
}
