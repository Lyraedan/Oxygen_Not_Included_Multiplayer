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

		// Grace period
		private bool _initialized = false;
		private float _initializationTime;
		private const float INITIAL_DELAY = 5f;

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
				// Skip if no clients connected
				if (MultiplayerSession.ConnectedPlayers.Count == 0)
					return;

				// Grace period after world load
				if (!_initialized)
				{
					_initializationTime = Time.unscaledTime;
					_initialized = true;
					return;
				}

				if (Time.unscaledTime - _initializationTime < INITIAL_DELAY)
					return;

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

		// Cached reflection field
		private static System.Reflection.FieldInfo _batteryJoulesField;

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
					if (_batteryJoulesField == null)
					{
						_batteryJoulesField = typeof(Battery).GetField("joulesAvailable", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
					}
					if (_batteryJoulesField != null)
					{
						_batteryJoulesField.SetValue(battery, packet.Value);
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
