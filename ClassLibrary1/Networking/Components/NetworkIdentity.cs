using KSerialization;
using ONI_MP.DebugTools;
using System.IO;

namespace ONI_MP.Networking.Components
{
	[SerializationConfig(MemberSerialization.OptIn)]
	public class NetworkIdentity : KMonoBehaviour, ISaveLoadableDetails
	{
		[Serialize]
		public int NetId;

		[SkipSaveFileSerialization]
		private bool IsRegistered = false;

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
			if (IsRegistered)
				return;

			if (Grid.WidthInCells == 0)
			{
				// DebugConsole.LogWarning($"[NetworkIdentity] Skipping registration for {gameObject.name} - Grid not ready");
				return;
			}

			// Try to handle deterministic ID for buildings first
			if (NetId == 0)
			{
				if (TryGetComponent<Building>(out var building))
				{
					int detId = NetIdHelper.GetDeterministicBuildingId(gameObject);
					if (detId != 0)
					{
						NetId = detId;
						// DebugConsole.Log($"[NetworkIdentity] Generated Deterministic NetId {detId} for building {gameObject.name}");
					}
				}
				else
				{
					int detId = NetIdHelper.GetDeterministicEntityId(gameObject);
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
			IsRegistered = true;
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
	}
}
