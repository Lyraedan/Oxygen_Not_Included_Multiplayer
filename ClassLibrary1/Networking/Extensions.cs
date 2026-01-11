using ONI_MP.Networking.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace ONI_MP.Networking
{
	public static class Extensions
	{
		public static NetworkIdentity GetNetIdentity(this MonoBehaviour behaviour)
		{
			if (behaviour.IsNullOrDestroyed() || behaviour.gameObject.IsNullOrDestroyed())
			{
				return null;
			}
			return behaviour.gameObject.GetNetIdentity();
		}
		public static NetworkIdentity GetNetIdentity(this GameObject go)
		{
			if (go.IsNullOrDestroyed())
			{
				return null;
			}

			if (go.TryGetComponent<NetworkIdentity>(out var identity))
				return identity;

			return go.AddComponent<NetworkIdentity>();
		}
		public static int GetNetId(this MonoBehaviour behaviour)
		{
			var identity = GetNetIdentity(behaviour);
			if (identity == null)
				return 0;
			return identity.NetId;
		}
	}
}
