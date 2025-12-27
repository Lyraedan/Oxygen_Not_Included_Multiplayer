using KSerialization;
using ONI_MP.DebugTools;
using System.IO;
using UnityEngine;

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

		public override void OnSpawn()
		{
			base.OnSpawn();
			RegisterIdentity();
		}

		public void RegisterIdentity()
		{
			if (Grid.WidthInCells == 0) 
			{
				// DebugConsole.LogWarning($"[NetworkIdentity] Skipping registration for {gameObject.name} - Grid not ready");
				return;
			}

			// Try to handle deterministic ID for buildings first
			if (NetId == 0)
			{
				var building = GetComponent<Building>();
				if (building != null)
				{
					int detId = NetIdHelper.GetDeterministicBuildingId(gameObject);
					if (detId != 0)
					{
						NetId = detId;
						// DebugConsole.Log($"[NetworkIdentity] Generated Deterministic NetId {detId} for building {gameObject.name}");
					}
				}
			}

			if (NetId == 0)
			{
				NetId = NetworkIdentityRegistry.Register(this);
				DebugConsole.Log($"[NetworkIdentity] Generated Random NetId {NetId} for {gameObject.name}");
			}
			else
			{
				NetworkIdentityRegistry.RegisterExisting(this, NetId);
				// DebugConsole.Log($"[NetworkIdentity] Registered Existing NetId {NetId} for {gameObject.name}");
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


		public override void OnCleanUp()
		{
			NetworkIdentityRegistry.Unregister(NetId);
			DebugConsole.Log($"[NetworkIdentity] Unregistered NetId {NetId} for {gameObject.name}");
			base.OnCleanUp();
		}

        public static bool TryAddNetworkIdentity_API(GameObject gameObject, out int netId)
        {
			if (!gameObject)
			{
				netId = 0;
				return false;
			}

            NetworkIdentity identity = gameObject.AddOrGet<NetworkIdentity>();
            if (identity)
            {
				netId = identity.NetId;
                return true;
            }

			netId = 0;
            return false;
        }

        public static bool TryGetNetworkIdentity_API(GameObject gameObject, out int netId)
        {
			if (!gameObject)
			{
				netId = 0;
				return false;
			}

            NetworkIdentity identity = gameObject.GetComponent<NetworkIdentity>();
            if (identity)
            {
				netId = identity.NetId;
                return true;
            }

			netId = 0;
            return false;
        }

		public static bool RegisterIdentity_API(GameObject entity, out int registered_netId)
		{
			if (!entity)
			{
				registered_netId = 0;
				return false;
			}

			NetworkIdentity identity = entity.GetComponent<NetworkIdentity>();
			if(identity)
			{
				registered_netId = identity.NetId;
				identity.RegisterIdentity();
				return true;
			}

			registered_netId = 0;
			return false;
		}

		public static bool OverrideNetId_API(GameObject entity, int new_netid, out int new_registered_netid)
		{
            if (!entity)
            {
				new_registered_netid = 0;
                return false;
            }

            NetworkIdentity identity = entity.GetComponent<NetworkIdentity>();
            if (identity)
            {
				identity.OverrideNetId(new_netid);
                new_registered_netid = new_netid;
                return true;
            }

			new_registered_netid = 0;
            return false;
        }
    }
}
