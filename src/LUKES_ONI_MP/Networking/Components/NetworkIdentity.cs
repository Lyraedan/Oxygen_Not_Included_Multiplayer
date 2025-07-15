using KSerialization;
using UnityEngine;
using ONI_MP.DebugTools;
using System.IO;
using ONI_MP.Misc;

namespace ONI_MP.Networking.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class NetworkIdentity : KMonoBehaviour, ISaveLoadableDetails
    {
        [Serialize]
        public int NetId;

        public void Serialize(BinaryWriter writer)
        {
            //DebugConsole.Log($"[NetworkIdentity] SERIALIZING: NetId = {NetId} on {gameObject.name}");
        }

        public void Deserialize(IReader reader)
        {
            //DebugConsole.Log($"[NetworkIdentity] DESERIALIZED: NetId = {NetId} on {gameObject.name}");
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            RegisterIdentity();
        }

        public void RegisterIdentity()
        {
            if (NetId == 0)
            {
                NetId = NetworkIdentityRegistry.Register(this);
            }
            else
            {
                NetworkIdentityRegistry.RegisterExisting(this, NetId);
            }
        }

        /// <summary>
        /// This will be primarily used when the host spawns in an object and the client and host need to sync the netid
        /// </summary>
        /// <param name="netIdOverride"></param>
        public void OverrideNetId(int netIdOverride)
        {
            // Unregister old NetId
            NetworkIdentityRegistry.Unregister(NetId);

            // Override internal value
            NetId = netIdOverride;

            // Re-register with new NetId
            NetworkIdentityRegistry.RegisterOverride(this, netIdOverride);

            DebugConsole.Log($"[NetworkIdentity] Overridden NetId. New NetId = {NetId} for {gameObject.name}");
        }


        protected override void OnCleanUp()
        {
            NetworkIdentityRegistry.Unregister(NetId);
            DebugConsole.Log($"[NetworkIdentity] Unregistered NetId {NetId} for {gameObject.name}");
            base.OnCleanUp();
        }
    }
}
