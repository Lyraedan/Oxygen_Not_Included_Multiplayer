using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ONI_MP.DebugTools;
using System;
using System.Linq;
using System.Reflection;

namespace ONI_MP.Misc
{
    public static class SafeSerializer
    {
        private static readonly JsonSerializerSettings SafeSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Error = (sender, args) =>
            {
                args.ErrorContext.Handled = true; // Ignore individual property failures
                DebugConsole.LogWarning($"[SafeSerializer] Ignoring serialization error for {args.ErrorContext.Member}: {args.ErrorContext.Error.Message}");
            },
            ContractResolver = new SafeContractResolver(),
            TypeNameHandling = TypeNameHandling.None // Avoid type resolution issues
        };

        /// <summary>
        /// Safely serializes any object, skipping Unity objects, loops, and broken callbacks.
        /// </summary>
        public static string ToJson(object obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, SafeSettings);
            }
            catch (Exception e)
            {
                DebugConsole.LogWarning($"[SafeSerializer] Failed to serialize object: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize to the specified type safely.
        /// </summary>
        public static object FromJson(string json, Type type)
        {
            try
            {
                return JsonConvert.DeserializeObject(json, type);
            }
            catch (Exception e)
            {
                DebugConsole.LogWarning($"[SafeSerializer] Failed to deserialize to {type}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize to the specified generic type safely.
        /// </summary>
        public static T FromJson<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                DebugConsole.LogWarning($"[SafeSerializer] Failed to deserialize to {typeof(T)}: {e.Message}");
                return default;
            }
        }
    }

    /// <summary>
    /// Custom contract resolver that safely handles problematic ONI types
    /// </summary>
    public class SafeContractResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            // Skip problematic ONI types that have serialization issues
            if (ShouldSkipCallbacks(objectType))
            {
                // Return a simple object contract that ignores callbacks
                var contract = base.CreateContract(objectType);
                if (contract is JsonObjectContract objectContract)
                {
                    objectContract.OnDeserializedCallbacks?.Clear();
                    objectContract.OnDeserializingCallbacks?.Clear();
                    objectContract.OnSerializedCallbacks?.Clear();
                    objectContract.OnSerializingCallbacks?.Clear();
                }
                return contract;
            }
            
            return base.CreateContract(objectType);
        }

        private bool ShouldSkipCallbacks(Type objectType)
        {
            if (objectType == null)
                return false;

            // Skip specific ONI classes with known serialization issues
            var problematicClasses = new[]
            {
                "Storage", "KPrefabID", "Assignable", "Component", "MonoBehaviour", 
                "GameObject", "Transform", "Behaviour", "Object"
            };

            if (problematicClasses.Contains(objectType.Name))
                return true;

            // Skip Unity and Klei namespaces entirely
            var namespaceName = objectType.Namespace ?? string.Empty;
            if (namespaceName.StartsWith("UnityEngine") ||
                namespaceName.StartsWith("Klei") ||
                namespaceName.StartsWith("Assets.Scripts"))
                return true;

            // Skip if it inherits from Unity's Component or MonoBehaviour (with null checks)
            try
            {
                var componentType = Type.GetType("UnityEngine.Component, UnityEngine");
                var monoBehaviourType = Type.GetType("UnityEngine.MonoBehaviour, UnityEngine");
                
                if (componentType != null && componentType.IsAssignableFrom(objectType))
                    return true;
                    
                if (monoBehaviourType != null && monoBehaviourType.IsAssignableFrom(objectType))
                    return true;
            }
            catch
            {
                // If we can't check Unity types, err on the side of caution for Unity-sounding names
                if (namespaceName.Contains("Unity") || objectType.Name.Contains("Unity"))
                    return true;
            }

            return false;
        }
    }
}
