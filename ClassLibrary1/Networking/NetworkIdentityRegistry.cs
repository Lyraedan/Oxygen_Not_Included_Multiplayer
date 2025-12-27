using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Networking
{
	public static class NetworkIdentityRegistry
	{
		private static readonly Dictionary<int, NetworkIdentity> identities = new Dictionary<int, NetworkIdentity>();
		private static readonly System.Random rng = new System.Random();

		public static int Register(NetworkIdentity entity)
		{
			int id;
			do
			{
				id = rng.Next(100000, 1000000000); // Stay below deterministic range (1B+)
			} while (identities.ContainsKey(id));

			identities[id] = entity;
			return id;
		}

		public static void Unregister(int netId)
		{
			if (identities.ContainsKey(netId))
			{
				identities.Remove(netId);
			} else
			{
				DebugConsole.LogWarning($"Tried to unregister an unregistered net id: {netId}");
			}
		}

		public static void RegisterExisting(NetworkIdentity entity, int netId)
		{
			if (!identities.ContainsKey(netId))
			{
				identities[netId] = entity;
				DebugConsole.Log($"[NetEntityRegistry] Registered existing entity with net id: {netId}");
			}
			//else
			//{
			//    DebugConsole.LogWarning($"[NetEntityRegistry] NetId {netId} already registered. Skipping duplicate registration.");
			//}
		}

		public static void RegisterOverride(NetworkIdentity entity, int netId)
		{
			if (identities.ContainsKey(netId))
			{
				DebugConsole.LogWarning($"[NetEntityRegistry] Overwriting existing entity for NetId {netId}");
				identities[netId] = entity;
			}
			else
			{
				identities.Add(netId, entity);
				DebugConsole.Log($"[NetEntityRegistry] Registered overridden NetId {netId} for {entity.name}");
			}
		}



		public static bool TryGet(int netId, out NetworkIdentity entity)
		{
			bool found = identities.TryGetValue(netId, out entity);
			if (!found)
			{
				DebugConsole.LogWarning($"[NetEntityRegistry] ERROR: Requested NetId {netId} NOT FOUND in registry. Current count: {identities.Count}");
			}
			return found;
		}

		public static void Clear()
		{
			identities.Clear();
		}

		public static IEnumerable<NetworkIdentity> AllIdentities => identities.Values;
    }
}
