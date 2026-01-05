using ONI_MP.DebugTools;
using UnityEngine;

namespace ONI_MP.Networking
{
	public static class NetIdHelper
	{
		/// <summary>
		/// Generates a deterministic NetID for a building based on its location and object layer.
		/// Range: 1,000,000,000+
		/// </summary>
		public static int GetDeterministicBuildingId(GameObject go)
		{
			if (go == null) return 0;

			int cell = Grid.PosToCell(go);
			if (!Grid.IsValidCell(cell)) return 0;

			if (!go.TryGetComponent<Building>(out var building))
				return cell.GetHashCode() ^ go.PrefabID().GetHashCode();

			return cell.GetHashCode() ^ go.PrefabID().GetHashCode() ^ building.Def.ObjectLayer.GetHashCode();
		}
		public static int GetDeterministicWorkableId(GameObject go)
		{
			if (go == null) return 0;

			int cell = Grid.PosToCell(go);
			if (!Grid.IsValidCell(cell)) return 0;

			if (!go.TryGetComponent<Workable>(out var workable))
				return 0;

			int hash = GetDeterministicEntityId(go,false,false) ^ workable.GetType().Name.GetHashCode() ^ ((int)workable.workTime).GetHashCode()
				^ workable.multitoolHitEffectTag.GetHashCode() ^ workable.multitoolContext.GetHashCode();
			int breakoff = 0;
			while (NetworkIdentityRegistry.Exists(hash + breakoff))
			{
				breakoff++;
			}
			hash += breakoff;
			DebugConsole.Log($"Registered workable {go.PrefabID().ToString()} with id: {hash} for workable type {workable.GetType().Name} at cell {cell}");
			return hash;
		}


		public static int GetDeterministicEntityId(GameObject go, bool breakOff = true, bool useCell = true)
		{
			if (go == null || !go.TryGetComponent<PrimaryElement>(out var primaryElement))
				return 0;

			int cell = Grid.PosToCell(go);
			if (!Grid.IsValidCell(cell))
				return 0;

			int hash = go.PrefabID().GetHashCode();
			if(useCell)
				hash = hash ^ cell.GetHashCode();
			hash = hash ^ go.GetProperName().GetHashCode() ^ primaryElement.ElementID.GetHashCode() ^ primaryElement.Mass.GetHashCode() ^ primaryElement.Temperature.GetHashCode();

			int breakoff = 0;
			if (breakOff)
			{
				while (NetworkIdentityRegistry.Exists(hash + breakoff))
				{
					breakoff++;
				}
			}
			hash += breakoff;
			if(breakOff)
				DebugConsole.Log($"Registered entity {go.PrefabID().ToString()} with id: {hash}");
			return hash;
		}
	}
}
