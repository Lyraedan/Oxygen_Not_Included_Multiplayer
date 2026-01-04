using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.World.Buildings
{
	internal class ComplexFabricatorSpawnProductPacket : IPacket
	{
		public int NetId, CompletedRecipeIdx;
		public ComplexFabricatorSpawnProductPacket() { }
		public ComplexFabricatorSpawnProductPacket(ComplexFabricator cf)
		{
			NetId = cf.GetNetId();
			CompletedRecipeIdx = cf.CurrentOrderIdx;
		}
		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(CompletedRecipeIdx);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			CompletedRecipeIdx = reader.ReadInt32();
		}


		public void OnDispatched()
		{
			if(NetworkIdentityRegistry.TryGetComponent<ComplexFabricator>(NetId, out var fab))
			{
				DebugConsole.LogWarning("Could not find ComplexFabricator for netId " + NetId);
				return;
			}

			ComplexRecipe complexRecipe = fab.recipe_list[CompletedRecipeIdx];
			DebugConsole.Log($"[ComplexFabricatorSpawnProductPacket] spawning product {complexRecipe.id} for {fab.name} with netId {NetId}");
			fab.SpawnOrderProduct(complexRecipe);
		}
	}
}
