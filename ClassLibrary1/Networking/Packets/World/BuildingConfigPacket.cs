using ONI_MP.Api.Networking.Packets.Architecture;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.World
{
	public class BuildingConfigPacket : IPacket
	{
		public int NetId;
		public int ConfigHash; // Hash of the property name (e.g. "Threshold", "Logic")
		public float Value;

		public static bool IsApplyingPacket = false;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(ConfigHash);
			writer.Write(Value);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			ConfigHash = reader.ReadInt32();
			Value = reader.ReadSingle();
		}

		public void OnDispatched()
		{
			if (NetworkIdentityRegistry.TryGet(NetId, out var identity) && identity != null)
			{
				// If Host received this from a client, apply it.
				// If Client received this from Host, apply it.
				// Implementation will be handled by a specific Syncer helper to keep Packet class clean?
				// Or just do it here for simplicity as per existing packets.

				try
				{
					IsApplyingPacket = true;
					ApplyConfig(identity.gameObject);
				}
				finally
				{
					IsApplyingPacket = false;
				}
				// else if (comp is LogicThresholdSwitch lts)
				// {
				//    lts.Threshold = Value;
				// }

				if (MultiplayerSession.IsHost)
				{
					// If Host received it, we applied it. Now we might need to broadcast it if the patch doesn't catch it.
					// Usually patches catch valid changes. 
					// However, we should be careful about loops. 
					// We'll rely on the patch to broadcast the change resulting from this application.
				}
			}
		}

		private void ApplyConfig(GameObject go)
		{
			if (go == null) return;

			// Handle Logic Switch (Signal Switch, etc.)
			var logicSwitch = go.GetComponent<LogicSwitch>();
			if (logicSwitch != null && ConfigHash == "LogicState".GetHashCode())
			{
				bool isOn = Value > 0.5f;
				if (logicSwitch.IsSwitchedOn != isOn)
				{
					HarmonyLib.Traverse.Create(logicSwitch).Method("HandleToggle").GetValue();
				}
				return;
			}

			// Handle Smart Battery (IActivationRangeTarget)
			var activationRange = go.GetComponent<IActivationRangeTarget>();
			if (activationRange != null)
			{
				if (ConfigHash == "Activate".GetHashCode()) activationRange.ActivateValue = Value;
				if (ConfigHash == "Deactivate".GetHashCode()) activationRange.DeactivateValue = Value;
				return; // Assume processed
			}

			// Handle Logic Sensors (IThresholdSwitch)
			// Note: SmartBattery also implements this but we prioritized ActivationRange. 
			// If SmartBattery receives "Activate", it hits above.
			// LogicThresholdSwitch receives "Threshold".

			// var thresholdSwitch = go.GetComponent<LogicThresholdSwitch>(); 
			// // Note: LogicThresholdSwitch is the component for Temp/Push/etc sensors. 
			// // IThresholdSwitch is the interface. 
			// if (thresholdSwitch != null && ConfigHash == "Threshold".GetHashCode())
			// {
			//     thresholdSwitch.Threshold = Value;
			//     return;
			// }

			// Handle Valves
			var valve = go.GetComponent<Valve>();
			if (valve != null && ConfigHash == "Rate".GetHashCode())
			{
				HarmonyLib.Traverse.Create(valve).Method("ChangeFlow", Value).GetValue();
				return;
			}
		}
	}
}
