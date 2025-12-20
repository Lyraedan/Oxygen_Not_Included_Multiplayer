using ONI_MP.Api.Networking;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.World;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class StructureStateSyncer : KMonoBehaviour
	{
		private float sendInterval = 0.5f; // Sync every 500ms
		private float timer;

		private Battery battery;
		private Generator generator;
		private Operational operational;
		private int cell;

		private float lastSentValue;
		private bool lastSentActive;

		public override void OnSpawn()
		{
			base.OnSpawn();

			if (!MultiplayerSession.InSession)
			{
				enabled = false;
				return;
			}

			cell = Grid.PosToCell(this);
			battery = GetComponent<Battery>();
			generator = GetComponent<Generator>();
			operational = GetComponent<Operational>();

			if (battery == null && generator == null)
			{
				// Not a relevant structure
				enabled = false;
			}
		}

		private void Update()
		{
			if (MultiplayerSession.IsHost)
			{
				HostUpdate();
			}
		}

		private void HostUpdate()
		{
			try
			{
				timer += Time.unscaledDeltaTime;
				if (timer < sendInterval) return;
				timer = 0f;

				float currentValue = 0f;
				bool currentActive = false;

				if (battery != null)
				{
					currentValue = battery.JoulesAvailable;
				}

				if (operational != null)
				{
					currentActive = operational.IsActive;
				}

				// Sync if changed significantly
				if (Mathf.Abs(currentValue - lastSentValue) > 0.1f || currentActive != lastSentActive)
				{
					lastSentValue = currentValue;
					lastSentActive = currentActive;

					var packet = new StructureStatePacket
					{
						Cell = cell,
						Value = currentValue,
						IsActive = currentActive
					};
					PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
				}
			}
			catch (System.Exception)
			{
				// Silently ignore - structure state may not be ready yet
			}
		}

		// Static handler for client-side reception
		public static void HandlePacket(StructureStatePacket packet)
		{
			if (!Grid.IsValidCell(packet.Cell)) return;

			GameObject go = Grid.Objects[packet.Cell, (int)Grid.SceneLayer.Building];
			if (go == null) return;

			// Apply state
			var battery = go.GetComponent<Battery>();
			if (battery != null)
			{
				// JoulesAvailable is read-only, set backing field via reflection
				try
				{
					var field = typeof(Battery).GetField("joulesAvailable", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
					if (field != null)
					{
						field.SetValue(battery, packet.Value);
					}
				}
				catch (System.Exception ex)
				{
					DebugConsole.LogError($"[StructureStateSyncer] Failed to set battery joules: {ex}");
				}
			}

			var operational = go.GetComponent<Operational>();
			if (operational != null)
			{
				operational.SetActive(packet.IsActive);
			}
		}
	}
}
