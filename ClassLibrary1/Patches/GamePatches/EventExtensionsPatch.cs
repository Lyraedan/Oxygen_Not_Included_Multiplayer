using HarmonyLib;
using ONI_MP.Api.Networking;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Events;
using System;
using UnityEngine;

namespace ONI_MP.Patches.GamePatches
{
	[HarmonyPatch(typeof(EventExtensions))]
	public static class EventExtensionsPatch
	{
		// This is a lil bit of a jank way of doing it. TODO Improve this.
		[HarmonyPrefix]
		[HarmonyPatch(nameof(EventExtensions.Trigger), new Type[] { typeof(GameObject), typeof(int), typeof(object) })]
		public static bool Prefix(GameObject go, int hash, object data)
		{
			//DebugConsole.Log($"[MP] Trigger intercepted: {go.name} -> {hash}, Data: {data}");

			KObject kObject = KObjectManager.Instance.Get(go);
			if (kObject != null && kObject.hasEventSystem)
			{
				kObject.GetEventSystem(out var eventSystem);
				eventSystem.Trigger(go, hash, data);
				if (MultiplayerSession.IsHost)
				{
					NetworkIdentity identity = go.GetComponent<NetworkIdentity>();
					if (identity != null)
					{
						// Skip syncing events with Unity object data - these cannot be serialized
						// and attempting to serialize them causes freezes
						object safeData = data;
						if (data != null)
						{
							var dataType = data.GetType();
							// Skip UnityEngine.Object types (MonoBehaviour, Component, GameObject, ScriptableObject, etc.)
							if (typeof(UnityEngine.Object).IsAssignableFrom(dataType))
							{
								safeData = null; // Don't try to serialize Unity objects
							}
							// Skip any type from Assembly-CSharp that inherits from UnityEngine.Object
							else if (dataType.Assembly.GetName().Name == "Assembly-CSharp" && 
									 typeof(UnityEngine.Object).IsAssignableFrom(dataType.BaseType))
							{
								safeData = null;
							}
						}
						
						var packet = new EventTriggeredPacket(identity.NetId, hash, safeData);
						PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
					}
				}
			}
			return false;
		}
	}
}
