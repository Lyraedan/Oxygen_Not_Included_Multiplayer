using HarmonyLib;
using ONI_MP.Api.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
	public class ConsumableStatePacket : IPacket
	{
		public int NetId;
		public List<string> ForbiddenIds = new List<string>();

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(ForbiddenIds.Count);
			foreach (var id in ForbiddenIds)
			{
				writer.Write(id ?? string.Empty);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			int count = reader.ReadInt32();
			ForbiddenIds = new List<string>(count);
			for (int i = 0; i < count; i++)
			{
				ForbiddenIds.Add(reader.ReadString());
			}
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
			if (!NetworkIdentityRegistry.TryGet(NetId, out var identity)) return;

			var consumer = identity.GetComponent<ConsumableConsumer>();
			if (consumer == null) return;

			// Apply forbidden tags
			// Accessing private 'forbiddenTags'
			var forbidden = Traverse.Create(consumer).Field("forbiddenTags").GetValue<HashSet<Tag>>();
			if (forbidden != null)
			{
				forbidden.Clear();
				foreach (var id in ForbiddenIds)
				{
					forbidden.Add(TagManager.Create(id));
				}

				// Refresh? consumer.forbiddenTags is a valid field usually.
				// Triggers usage refresh?
				// consumer.forbiddenTags is used by IsPermitted.
			}
		}
	}
}
