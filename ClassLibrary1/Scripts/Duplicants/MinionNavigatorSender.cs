using ONI_MP.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Profiling;

namespace ONI_MP.Scripts.Duplicants
{
	internal class MinionNavigatorSender : KMonoBehaviour
	{
		//not yet used.
		[MyCmpGet] KPrefabID kpref;
		[MyCmpGet] Navigator navigator;
		public override void OnSpawn()
		{
			Profiler.Active.Scope();

			base.OnSpawn();

			//Subscribe((int)GameHashes.ObjectMovementStateChanged,);
			Subscribe((int)GameHashes.NavigationCellChanged, OnPathChanged);
			Subscribe((int)GameHashes.PathAdvanced, OnPathAdvanced);

		}
		void OnPathChanged(object _)
		{
			Profiler.Active.Scope();

			if(MultiplayerSession.InSession && MultiplayerSession.IsHost)
			{

			}
		}
		void OnPathAdvanced(object _) {
		}

	}
}
