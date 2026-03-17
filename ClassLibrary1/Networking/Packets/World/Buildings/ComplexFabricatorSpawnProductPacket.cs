using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.World.Buildings
{
	internal class ComplexFabricatorSpawnProductPacket : IPacket
	{
		public int NetId, CompletedRecipeIdx;
		public ComplexFabricatorSpawnProductPacket() { }
		public ComplexFabricatorSpawnProductPacket(ComplexFabricator cf)
		{
			Profiler.Active.Scope();

			NetId = cf.GetNetId();
			CompletedRecipeIdx = cf.CurrentOrderIdx;
		}
		public void Serialize(BinaryWriter writer)
		{
			Profiler.Active.Scope();

			writer.Write(NetId);
			writer.Write(CompletedRecipeIdx);
		}

		public void Deserialize(BinaryReader reader)
		{
			Profiler.Active.Scope();

			NetId = reader.ReadInt32();
			CompletedRecipeIdx = reader.ReadInt32();
		}


		public void OnDispatched()
		{
			Profiler.Active.Scope();

			if(!NetworkIdentityRegistry.TryGetComponent<ComplexFabricator>(NetId, out var fab))
			{
				DebugConsole.LogWarning("[ComplexFabricatorSpawnProductPacket] Could not find ComplexFabricator for netId " + NetId);
				return;
			}

			ComplexRecipe complexRecipe = fab.recipe_list[CompletedRecipeIdx];
			DebugConsole.Log($"[ComplexFabricatorSpawnProductPacket] spawning product {complexRecipe.id} for {fab.name} with netId {NetId}");
			fab.SpawnOrderProduct(complexRecipe);
		}
	}
}
