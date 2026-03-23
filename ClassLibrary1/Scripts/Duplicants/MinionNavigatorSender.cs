using ONI_MP.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Profiling;

namespace ONI_MP.Scripts.Duplicants
{
	internal class MinionNavigatorSender : KMonoBehaviour
	{
		//not yet used.
		[MyCmpGet] KPrefabID kpref;
		[MyCmpGet] Navigator navigator;
		public override void OnSpawn()
		{
			Profiler.Scope();

			base.OnSpawn();

			//Subscribe((int)GameHashes.ObjectMovementStateChanged,);
			Subscribe((int)GameHashes.NavigationCellChanged, OnPathChanged);
			Subscribe((int)GameHashes.PathAdvanced, OnPathAdvanced);

		}
		void OnPathChanged(object _)
		{
			Profiler.Scope();

			if(MultiplayerSession.InSession && MultiplayerSession.IsHost)
			{

			}
		}
		void OnPathAdvanced(object _) {
		}

	}
}
