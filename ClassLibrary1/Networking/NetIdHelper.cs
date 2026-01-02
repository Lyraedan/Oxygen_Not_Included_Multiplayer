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

			if(!go.TryGetComponent<Building>(out var building))
				return cell.GetHashCode() ^ go.PrefabID().GetHashCode();

			return cell.GetHashCode() ^ go.PrefabID().GetHashCode() ^ building.Def.ObjectLayer.GetHashCode();
		}

		public static int GetDeterministicEntityId(GameObject go)
		{
			if (go == null || !go.TryGetComponent<PrimaryElement>(out var primaryElement)) 
				return 0;

			int cell = Grid.PosToCell(go);
			if (!Grid.IsValidCell(cell)) 
				return 0;


			int hash = go.transform.position.GetHashCode() ^ go.PrefabID().GetHashCode() ^ go.GetProperName().GetHashCode() ^ primaryElement.ElementID.GetHashCode() ^ primaryElement.Mass.GetHashCode();
			if (NetworkIdentityRegistry.Exists(hash))
				return 0;

			return hash;
		}
	}
}
