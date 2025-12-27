using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.Networking;
using Shared.Helpers;
using Steamworks;
using UnityEngine;

namespace ONI_MP_API.Networking
{
    public static class NetworkIdentityAPI
    {
        static bool Init()
        {
            if (typesInitialized)
                return true;

            if (!ReflectionHelper.TryCreateDelegate<TryAddNetworkIdentityDelegate>("ONI_MP.Networking.Components.NetworkIdentity, ONI_MP", "TryAddNetworkIdentity_API", [typeof(GameObject), typeof(int)], out _TryAddNetworkIdentity, [ArgumentType.Normal, ArgumentType.Out]))
                return false;

            if (!ReflectionHelper.TryCreateDelegate<TryGetNetworkIdentityDelegate>("ONI_MP.Networking.Components.NetworkIdentity, ONI_MP", "TryGetNetworkIdentity_API", [typeof(GameObject), typeof(int)], out _TryGetNetworkIdentity, [ArgumentType.Normal, ArgumentType.Out]))
                return false;

            if (!ReflectionHelper.TryCreateDelegate<RegisterIdentityDelegate>("ONI_MP.Networking.Components.NetworkIdentity, ONI_MP", "RegisterIdentity_API", [typeof(GameObject), typeof(int)], out _RegisterIdentity, [ArgumentType.Normal, ArgumentType.Out]))
                return false;

            if (!ReflectionHelper.TryCreateDelegate<OverrideNetIdDelegate>("ONI_MP.Networking.Components.NetworkIdentity, ONI_MP", "OverrideNetId_API", [typeof(GameObject), typeof(int)], out _OverrideNetId, [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out]))
                return false;

            typesInitialized = true;
            return true;
        }
        static bool typesInitialized = false;

        static TryAddNetworkIdentityDelegate? _TryAddNetworkIdentity = null;
        delegate bool TryAddNetworkIdentityDelegate(GameObject gameObject, out int netId);

        static TryGetNetworkIdentityDelegate? _TryGetNetworkIdentity = null;
        delegate bool TryGetNetworkIdentityDelegate(GameObject gameObject, out int netId);

        static RegisterIdentityDelegate? _RegisterIdentity = null;
        delegate bool RegisterIdentityDelegate(GameObject entity, out int registered_netId);

        static OverrideNetIdDelegate? _OverrideNetId = null;
        delegate bool OverrideNetIdDelegate(GameObject entity, int new_netid, out int new_registered_netid);
    }
}
