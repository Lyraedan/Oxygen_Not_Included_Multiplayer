using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System.IO;
using HarmonyLib;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
	public class ConsumablePermissionPacket : IPacket
	{
		public int NetId;
		public string ConsumableId;
		public bool IsAllowed;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(ConsumableId ?? string.Empty);
			writer.Write(IsAllowed);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			ConsumableId = reader.ReadString();
			IsAllowed = reader.ReadBoolean();
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
			{
				Apply();
				PacketSender.SendToAllClients(this);
			}
			else
			{
				Apply();
			}
		}

		private void Apply()
		{
			if (!NetworkIdentityRegistry.TryGet(NetId, out var identity) || identity == null)
			{
				DebugConsole.LogWarning($"[ConsumablePermissionPacket] NetId {NetId} not found.");
				return;
			}

			var consumer = identity.GetComponent<ConsumableConsumer>();
			if (consumer == null)
			{
				DebugConsole.LogWarning($"[ConsumablePermissionPacket] NetId {NetId} missing ConsumableConsumer.");
				return;
			}

			IsApplying = true;
			try
			{
				// ConsumableConsumer.SetPermitted(string consumable_id, bool is_allowed)
				consumer.SetPermitted(ConsumableId, IsAllowed);
				ManagementMenu.Instance.consumablesScreen.MarkRowsDirty();
				// DebugConsole.Log($"[ConsumablePermissionPacket] Set {ConsumableId} to {IsAllowed} for {identity.name}");
			}
			finally
			{
				IsApplying = false;
			}
		}

		public static bool IsApplying = false;
	}
}
