using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using System.IO;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
	public class DuplicantPriorityPacket : IPacket
	{
		public int NetId;
		public string ChoreGroupId;
		public int Priority;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(ChoreGroupId ?? string.Empty);
			writer.Write(Priority);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			ChoreGroupId = reader.ReadString();
			Priority = reader.ReadInt32();
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
			{
				// Host receives from client, apply and broadcast
				Apply();

				// Broadcast to other clients
				if (NetworkIdentityRegistry.TryGet(NetId, out var identity))
				{
					PacketSender.SendToAllClients(this);
				}
			}
			else
			{
				// Client receives from host
				Apply();
			}
		}

		private void Apply()
		{
			if (!NetworkIdentityRegistry.TryGet(NetId, out var identity))
			{
				DebugConsole.LogWarning($"[DuplicantPriorityPacket] NetId {NetId} not found.");
				return;
			}

			var consumer = identity.GetComponent<ChoreConsumer>();
			if (consumer == null)
			{
				DebugConsole.LogWarning($"[DuplicantPriorityPacket] NetId {NetId} has no ChoreConsumer.");
				return;
			}

			// Find the ChoreGroup
			ChoreGroup targetGroup = null;
			foreach (var group in Db.Get().ChoreGroups.resources)
			{
				if (group.Id == ChoreGroupId)
				{
					targetGroup = group;
					break;
				}
			}

			if (targetGroup != null)
			{
				// SetPersonalPriority expects (ChoreGroup, int)
				// We must ensure we don't trigger infinite loop if we patch this method.
				// We'll handle re-entrancy in the Patch or use a flag here?
				// The Patch usually checks "IsApplying".
				// Let's assume we add a static flag in this packet class or the patch class.

				IsApplying = true;
				try
				{
					consumer.SetPersonalPriority(targetGroup, Priority);
					DebugConsole.Log($"[DuplicantPriorityPacket] Applied {ChoreGroupId} = {Priority} to {identity.name}");
				}
				finally
				{
					IsApplying = false;
				}
			}
			else
			{
				DebugConsole.LogWarning($"[DuplicantPriorityPacket] ChoreGroup {ChoreGroupId} not found.");
			}
		}

		public static bool IsApplying = false;
	}
}
