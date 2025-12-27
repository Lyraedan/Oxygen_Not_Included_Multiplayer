using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	/// <summary>
	/// Packet to spawn entities (duplicants or items) on clients with matching NetIds.
	/// Sent from host when an entity is spawned (e.g., from Telepad).
	/// </summary>
	public class EntitySpawnPacket : IPacket
	{
		public int NetId;
		public bool IsDuplicant;

		// Duplicant data
		public string Name;
		public string PersonalityId;
		public List<string> TraitIds;

		// Item data
		public string ItemId;
		public float Quantity;

		// Position
		public float PosX;
		public float PosY;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(IsDuplicant);

			if (IsDuplicant)
			{
				writer.Write(Name ?? "");
				writer.Write(PersonalityId ?? "");

				writer.Write(TraitIds?.Count ?? 0);
				if (TraitIds != null)
				{
					foreach (var traitId in TraitIds)
					{
						writer.Write(traitId ?? "");
					}
				}
			}
			else
			{
				writer.Write(ItemId ?? "");
				writer.Write(Quantity);
			}

			writer.Write(PosX);
			writer.Write(PosY);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			IsDuplicant = reader.ReadBoolean();

			if (IsDuplicant)
			{
				Name = reader.ReadString();
				PersonalityId = reader.ReadString();

				int traitCount = reader.ReadInt32();
				TraitIds = new List<string>(traitCount);
				for (int i = 0; i < traitCount; i++)
				{
					TraitIds.Add(reader.ReadString());
				}
			}
			else
			{
				ItemId = reader.ReadString();
				Quantity = reader.ReadSingle();
			}

			PosX = reader.ReadSingle();
			PosY = reader.ReadSingle();
		}

		public void OnDispatched()
		{
			DebugConsole.Log($"[EntitySpawnPacket] OnDispatched called - NetId {NetId}, IsDuplicant={IsDuplicant}, IsHost={MultiplayerSession.IsHost}");

			// Only clients should process this
			if (MultiplayerSession.IsHost) return;

			DebugConsole.Log($"[EntitySpawnPacket] Client: Received spawn for NetId {NetId}, IsDuplicant={IsDuplicant}");

			try
			{
				var telepad = UnityEngine.Object.FindObjectOfType<Telepad>();
				if (telepad == null)
				{
					DebugConsole.LogWarning("[EntitySpawnPacket] Cannot find Telepad");
					return;
				}

				UnityEngine.GameObject spawnedGO = null;

				if (IsDuplicant)
				{
					var personality = Db.Get().Personalities.TryGet(PersonalityId);
					if (personality == null) personality = Db.Get().Personalities.TryGet("Hassan");

					var stats = new MinionStartingStats(personality);
					stats.Name = Name;

					if (TraitIds != null)
					{
						stats.Traits.Clear();
						foreach (var traitId in TraitIds)
						{
							var trait = Db.Get().traits.TryGet(traitId);
							if (trait != null) stats.Traits.Add(trait);
						}
					}

					// Spawn via telepad
					var pos = new UnityEngine.Vector3(PosX, PosY, 0);
					spawnedGO = stats.Deliver(pos);

					DebugConsole.Log($"[EntitySpawnPacket] Client: Spawned duplicant {Name} at ({PosX}, {PosY})");
				}
				else
				{
					// Don't use pkg.Deliver() on client as it causes telepad animation to freeze
					// Spawn items directly instead
					DebugConsole.Log($"[EntitySpawnPacket] Client: Spawning care package {ItemId} x{Quantity} directly");

					try
					{
						var pos = new UnityEngine.Vector3(PosX, PosY, 0);
						var prefab = Assets.GetPrefab(new Tag(ItemId));

						if (prefab != null)
						{
							// Spawn the item(s) directly
							spawnedGO = Util.KInstantiate(prefab, pos);
							if (spawnedGO != null)
							{
								spawnedGO.SetActive(true);

								// Set the amount/quantity if it has a PrimaryElement
								var primaryElement = spawnedGO.GetComponent<PrimaryElement>();
								if (primaryElement != null)
								{
									primaryElement.Mass = Quantity;
								}

								DebugConsole.Log($"[EntitySpawnPacket] Client: Directly spawned {ItemId} x{Quantity} at ({PosX}, {PosY})");
							}
						}
						else
						{
							// Fallback: maybe it's an egg or critter - try using CarePackageInfo.Deliver as last resort
							DebugConsole.LogWarning($"[EntitySpawnPacket] Client: Prefab not found for {ItemId}, trying fallback...");
							var pkg = new CarePackageInfo(ItemId, Quantity, null);
							spawnedGO = pkg.Deliver(pos);
						}
					}
					catch (System.Exception ex)
					{
						DebugConsole.LogError($"[EntitySpawnPacket] Client: Direct spawn failed: {ex.Message}");
						// Fallback to pkg.Deliver
						var pkg = new CarePackageInfo(ItemId, Quantity, null);
						var pos = new UnityEngine.Vector3(PosX, PosY, 0);
						spawnedGO = pkg.Deliver(pos);
					}
				}

				// Set the NetId to match the host's entity
				if (spawnedGO != null)
				{
					var identity = spawnedGO.GetComponent<NetworkIdentity>();
					if (identity == null)
					{
						identity = spawnedGO.AddComponent<NetworkIdentity>();
					}

					// Override the NetId to match host
					identity.OverrideNetId(NetId);

					// Just use OverrideNetId
					//identity.NetId = NetId;
					//NetworkIdentityRegistry.Register(identity);

					DebugConsole.Log($"[EntitySpawnPacket] Client: Registered entity with NetId {NetId}");
				}
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[EntitySpawnPacket] Failed to spawn: {ex.Message}");
			}
		}
	}
}
