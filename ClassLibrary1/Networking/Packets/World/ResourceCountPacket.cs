using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.Networking.Synchronization;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	public class ResourceCountPacket : IPacket
	{

		// Using a dictionary is heavy, so let's Serialize a list of tag hashes/names and amounts.
		// Tag (string) -> Amount (float)
		public Dictionary<string, float> Resources = new Dictionary<string, float>();

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Resources.Count);
			foreach (var kvp in Resources)
			{
				writer.Write(kvp.Key);
				writer.Write(kvp.Value);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			Resources.Clear();
			for (int i = 0; i < count; i++)
			{
				string key = reader.ReadString();
				float val = reader.ReadSingle();
				Resources[key] = val;
			}
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost) return;
			Apply();
		}

		private void Apply()
		{
			// Update local cache for the patch to use
			ResourceSyncer.ClientResources = Resources;

			// Ensure these resources are "Discovered" so they show up in the UI list
			if (DiscoveredResources.Instance != null)
			{
				foreach (var kvp in Resources)
				{
					Tag tag = TagManager.Create(kvp.Key);
					// DiscoveredResources.Instance.Discover(tag); 
					// To avoid spamming notifications or issues, we can check if already discovered.
					if (!DiscoveredResources.Instance.IsDiscovered(tag))
					{
						try
						{
							DiscoveredResources.Instance.Discover(tag);
						}
						catch
						{
							// Ignore specific discovery failures
						}
					}
				}
			}
		}
	}
}
